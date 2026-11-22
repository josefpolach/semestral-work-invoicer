using Invoicing.Core.Documents;
using Invoicing.Core.Services;
using Invoicing.Core.Validation;
using Invoicing.Data;
using Invoicing.Tests.TestHelpers;
using Microsoft.Data.Sqlite;

namespace Invoicing.Tests.Data;

public class DocumentRepositoryTests
{
    [Fact]
    public async Task SaveDraftAsync_RoundTripsDecimalTextAndReplacesLines()
    {
        using var database = new TemporaryDatabase();
        await database.CreateAsync();
        var factory = new SqliteConnectionFactory(database.Path);
        var customers = new CustomerRepository(factory);
        var customer = TestData.Customer();
        await customers.SaveAsync(customer);
        var repository = new DocumentRepository(factory);
        var invoice = TestData.Invoice(customer);
        invoice.ReplaceLines([new("First", 0.1m, "pc", 1234.56m, 21m, -25.03m), TestData.Lines[0]]);
        await repository.SaveDraftAsync(invoice);
        var stored = Assert.IsType<TaxInvoice>(await repository.GetByIdAsync(invoice.Id));
        Assert.Equal(new[] { "First", "Consulting" }, stored.Lines.Select(line => line.Description));
        Assert.Equal((0.1m, 1234.56m, -25.03m),
            (stored.Lines[0].Quantity, stored.Lines[0].UnitPriceWithoutVat, stored.Lines[0].DiscountPercent));
        stored.ReplaceLines([TestData.Lines[1]]);
        await repository.SaveDraftAsync(stored);
        var updated = Assert.IsType<TaxInvoice>(await repository.GetByIdAsync(stored.Id));
        Assert.Equal("Service", Assert.Single(updated.Lines).Description);
        await repository.DeleteDraftAsync(updated);
        Assert.Null(await repository.GetByIdAsync(stored.Id));
    }

    [Fact]
    public async Task IssueAsync_AssignsYearlyNumbersAndKeepsSnapshots()
    {
        using var database = new TemporaryDatabase();
        await database.CreateAsync();
        var factory = new SqliteConnectionFactory(database.Path);
        var customers = new CustomerRepository(factory);
        var customer = TestData.Customer();
        await customers.SaveAsync(customer);
        var suppliers = new SupplierRepository(factory);
        await suppliers.SaveAsync(TestData.Supplier());
        var documents = new DocumentRepository(factory);
        var service = new DocumentService(suppliers, documents, TestData.Rates);
        var first = TestData.Invoice(customer);
        first.TaxableSupplyDate = TestData.Today.AddDays(-16);
        var warning = Assert.Single(await service.IssueAsync(first));
        Assert.Equal(ValidationSeverity.Warning, warning.Severity);
        Assert.Equal("2026-0001", first.Number);

        var second = TestData.Invoice(customer);
        await service.IssueAsync(second);
        Assert.Equal("2026-0002", second.Number);
        await CheckOtherSeriesAsync(service, first, customer.Id);

        var name = customer.Name;
        customer.Name = "Updated";
        await customers.SaveAsync(customer);
        var stored = Assert.IsType<TaxInvoice>(await documents.GetByIdAsync(first.Id));
        Assert.Equal(name, stored.Customer?.Name);
        Assert.Equal("CZ6508000000192000145399", stored.Supplier?.Iban);
    }

    [Fact]
    public async Task IssueAsync_ForeignKeyFailure_RollsBackNumberAndState()
    {
        using var database = new TemporaryDatabase();
        await database.CreateAsync();
        var factory = new SqliteConnectionFactory(database.Path);
        var repository = new DocumentRepository(factory);
        var invalid = TestData.Invoice();
        await Assert.ThrowsAsync<SqliteException>(() => repository.IssueAsync(invalid, TestData.Supplier()));
        Assert.Equal(DocumentStatus.Draft, invalid.Status);
        Assert.Equal(0, invalid.Id);
        var customer = TestData.Customer();
        await new CustomerRepository(factory).SaveAsync(customer);
        var valid = TestData.Invoice(customer);
        await repository.IssueAsync(valid, TestData.Supplier());
        Assert.Equal("2026-0001", valid.Number);
    }

    [Fact]
    public async Task IssueAsync_ConcurrentInvoicesUseConsecutiveNumbers()
    {
        using var database = new TemporaryDatabase();
        await database.CreateAsync();
        var factory = new SqliteConnectionFactory(database.Path);
        var customer = TestData.Customer();
        await new CustomerRepository(factory).SaveAsync(customer);
        var taxInvoice = TestData.Invoice(customer);
        var nonVatInvoice = new NonVatInvoice(TestData.Header(customer), TestData.Lines);
        var repository = new DocumentRepository(factory);
        await Task.WhenAll(
            Task.Run(() => repository.IssueAsync(taxInvoice, TestData.Supplier())),
            Task.Run(() => repository.IssueAsync(nonVatInvoice, TestData.Supplier(false))));
        var stored = await repository.GetAllAsync();
        Assert.Equal(new[] { "2026-0001", "2026-0002" },
            stored.Select(document => document.Number).Order(StringComparer.Ordinal));
    }

    private static async Task CheckOtherSeriesAsync(DocumentService service, TaxInvoice first, int customerId)
    {
        var credit = await service.CreateCreditNoteAsync(first, TestData.Today);
        credit.CorrectionReason = "Quantity correction";
        await service.IssueAsync(credit);
        Assert.Equal("OD-2026-0001", credit.Number);
        var customer = TestData.Customer();
        customer.Id = customerId;
        var nextYear = TestData.Today.AddYears(1);
        var next = new TaxInvoice(DocumentHeader.NewDraft(customer, nextYear, nextYear.AddDays(14)), TestData.Lines, nextYear);
        await service.IssueAsync(next);
        Assert.Equal("2027-0001", next.Number);
    }
}
