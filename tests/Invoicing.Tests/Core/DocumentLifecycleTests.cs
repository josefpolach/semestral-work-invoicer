using Invoicing.Core.Documents;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class DocumentLifecycleTests
{
    [Fact]
    public void Issue_Draft_SnapshotsPartiesAndPreventsEdits()
    {
        var customer = TestData.Customer();
        var supplier = TestData.Supplier();
        var invoice = TestData.Invoice(customer);
        invoice.Issue("2026-0001", supplier);
        var expected = (customer.Name, supplier.Name);
        customer.Name = "New customer";
        supplier.Name = "New supplier";
        Assert.Equal(expected, (invoice.Customer?.Name, invoice.Supplier?.Name));
        Assert.Equal("2026-0001", invoice.Number);
        Assert.Equal(DocumentStatus.Issued, invoice.Status);
        Assert.Throws<InvalidOperationException>(() => invoice.Issue("2026-0002", supplier));
        Assert.Throws<InvalidOperationException>(() => invoice.IssueDate = TestData.Today);
        Assert.Throws<InvalidOperationException>(() => invoice.DueDate = TestData.Today);
        Assert.Throws<InvalidOperationException>(() => invoice.Customer = customer);
        Assert.Throws<InvalidOperationException>(() => invoice.TaxableSupplyDate = TestData.Today);
        Assert.Throws<InvalidOperationException>(() => invoice.ReplaceLines([]));
    }

    [Fact]
    public void ChangeStatus_AllowedTransitions_UpdatesState()
    {
        var tax = TestData.Issued(DocumentKind.TaxInvoice);
        var nonVat = TestData.Issued(DocumentKind.NonVatInvoice);
        Assert.Throws<InvalidOperationException>(tax.Cancel);
        tax.MarkAsPaid();
        nonVat.Cancel();
        Assert.Equal(DocumentStatus.Paid, tax.Status);
        Assert.Equal(DocumentStatus.Cancelled, nonVat.Status);
    }

    [Fact]
    public void ChangeStatus_InvalidTransitions_Throws()
    {
        var draft = TestData.Invoice();
        var credit = Assert.IsType<CreditNote>(TestData.Issued(DocumentKind.CreditNote));
        Assert.Throws<InvalidOperationException>(draft.MarkAsPaid);
        Assert.Throws<InvalidOperationException>(draft.Cancel);
        Assert.Throws<InvalidOperationException>(credit.MarkAsPaid);
        Assert.Throws<InvalidOperationException>(() => credit.CorrectionReason = "Other reason");
    }

    [Fact]
    public void IsOverdue_DependsOnKindDateAndStatus()
    {
        var invoice = TestData.Issued(DocumentKind.TaxInvoice);
        Assert.True(invoice.IsOverdue(TestData.Today.AddDays(15)));
        Assert.False(invoice.IsOverdue(TestData.Today.AddDays(14)));
        invoice.MarkAsPaid();
        Assert.False(invoice.IsOverdue(TestData.Today.AddDays(15)));
        Assert.False(TestData.Issued(DocumentKind.CreditNote).IsOverdue(TestData.Today.AddDays(15)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateCreditNote_IssuedOrPaid_CopiesCustomerAndNegatesLines(bool paid)
    {
        var invoice = Assert.IsType<TaxInvoice>(TestData.Issued(DocumentKind.TaxInvoice));
        if (paid)
        {
            invoice.MarkAsPaid();
        }

        var correction = invoice.CreateCreditNote(TestData.Today, 14);
        Assert.Equal(new DocumentReference(invoice.Id, "2026-0001"), correction.OriginalInvoice);
        Assert.Equal(DocumentStatus.Draft, correction.Status);
        Assert.Equal(TestData.Today, correction.IssueDate);
        Assert.Equal(TestData.Today, correction.TaxableSupplyDate);
        Assert.Equal(TestData.Today.AddDays(14), correction.DueDate);
        Assert.Equal(invoice.Lines.Select(line => -line.Quantity), correction.Lines.Select(line => line.Quantity));
        Assert.Equal(invoice.Customer?.Name, correction.Customer?.Name);
        Assert.Throws<InvalidOperationException>(() => TestData.Invoice().CreateCreditNote(TestData.Today, 14));
    }
}
