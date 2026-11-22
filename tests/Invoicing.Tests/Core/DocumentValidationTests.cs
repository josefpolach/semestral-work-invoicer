using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using Invoicing.Core.Validation;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class DocumentValidationTests
{
    [Theory]
    [InlineData(DocumentKind.TaxInvoice)]
    [InlineData(DocumentKind.NonVatInvoice)]
    [InlineData(DocumentKind.CreditNote)]
    public void Validate_CompleteDocument_ReturnsNoIssues(DocumentKind kind)
    {
        var document = TestData.Issued(kind);
        Assert.Empty(document.Validate(TestData.Supplier(kind != DocumentKind.NonVatInvoice), TestData.Rates));
    }

    [Theory]
    [InlineData("", 1, 1, 21, 0, ValidationCode.DescriptionMissing)]
    [InlineData("Item", 0, 1, 21, 0, ValidationCode.QuantityNotPositive)]
    [InlineData("Item", 1, -1, 21, 0, ValidationCode.UnitPriceNegative)]
    [InlineData("Item", 1, 1, 0, 0, ValidationCode.VatRateNotAllowed)]
    [InlineData("Item", 1, 1, 21, 101, ValidationCode.DiscountOutOfRange)]
    [InlineData("Item", 1, 1, 21, -1, ValidationCode.DiscountOutOfRange)]
    public void Validate_InvalidLine_IdentifiesPosition(string description, int quantity, int price, int rate,
        int discount, ValidationCode code)
    {
        var invoice = TestData.Invoice();
        invoice.ReplaceLines([TestData.Lines[0], new(description, quantity, "pc", price, rate, discount)]);
        Assert.Equal(new ValidationMessage(code, ValidationTarget.Line, LinePosition: 2),
            Assert.Single(invoice.Validate(TestData.Supplier(), TestData.Rates)));
    }

    [Theory]
    [InlineData(15, false)]
    [InlineData(16, true)]
    public void Validate_TaxInvoiceDeadline_WarnsAfterFifteenDays(int days, bool warns)
    {
        var invoice = TestData.Invoice();
        invoice.TaxableSupplyDate = TestData.Today.AddDays(-days);
        var result = invoice.Validate(TestData.Supplier(), TestData.Rates);
        if (warns)
        {
            Assert.Equal(new ValidationMessage(ValidationCode.IssuedLongAfterTaxableSupply, ValidationTarget.Document,
                ValidationSeverity.Warning), Assert.Single(result));
        }
        else
        {
            Assert.Empty(result);
        }
    }

    [Theory]
    [InlineData(ValidationCode.IssueDateMissing)]
    [InlineData(ValidationCode.DueDateMissing)]
    [InlineData(ValidationCode.DueDateBeforeIssueDate)]
    [InlineData(ValidationCode.TaxableSupplyDateMissing)]
    [InlineData(ValidationCode.NoLines)]
    public void Validate_MissingOrInvalidHeader_ReportsExpectedCode(ValidationCode code)
    {
        var invoice = TestData.Invoice();
        switch (code)
        {
            case ValidationCode.IssueDateMissing: invoice.IssueDate = null; break;
            case ValidationCode.DueDateMissing: invoice.DueDate = null; break;
            case ValidationCode.DueDateBeforeIssueDate: invoice.DueDate = TestData.Today.AddDays(-1); break;
            case ValidationCode.TaxableSupplyDateMissing: invoice.TaxableSupplyDate = null; break;
            case ValidationCode.NoLines: invoice.ReplaceLines([]); break;
        }
        Assert.Equal(new ValidationMessage(code, ValidationTarget.Document),
            Assert.Single(invoice.Validate(TestData.Supplier(), TestData.Rates)));
    }

    [Fact]
    public void Validate_MissingParties_ReportsBothTargets()
    {
        var draft = TestData.Invoice();
        draft.Customer = null;
        Assert.Equal(new[] { new ValidationMessage(ValidationCode.Missing, ValidationTarget.Supplier),
            new ValidationMessage(ValidationCode.Missing, ValidationTarget.Customer) }, draft.Validate(null, TestData.Rates));
    }

    [Fact]
    public void Validate_WrongSupplierVatStatus_RejectsBothInvoiceKinds()
    {
        var tax = TestData.Invoice();
        var nonVat = new NonVatInvoice(TestData.Header(), TestData.Lines);
        Assert.Equal(ValidationCode.MustBeVatPayer, Assert.Single(tax.Validate(TestData.Supplier(false), TestData.Rates)).Code);
        Assert.Equal(ValidationCode.MustNotBeVatPayer, Assert.Single(nonVat.Validate(TestData.Supplier(), TestData.Rates)).Code);
        Assert.All(nonVat.Lines, line => Assert.Equal(0m, line.VatRate));
    }

    [Fact]
    public void Validate_CreditNoteRules_ReportsMissingReferenceReasonAndZeroQuantity()
    {
        var credit = new CreditNote(TestData.Header(), [new("Item", 0, "pc", 1, 21, 0)],
            TestData.Today.AddDays(-20), null);
        var messages = credit.Validate(TestData.Supplier(), TestData.Rates);
        Assert.Equal(new[] { new ValidationMessage(ValidationCode.QuantityZero, ValidationTarget.Line, LinePosition: 1),
            new ValidationMessage(ValidationCode.OriginalInvoiceMissing, ValidationTarget.Document),
            new ValidationMessage(ValidationCode.CorrectionReasonMissing, ValidationTarget.Document) }, messages);
    }

    [Theory]
    [InlineData(ValidationCode.NameMissing)]
    [InlineData(ValidationCode.AddressIncomplete)]
    [InlineData(ValidationCode.IcoMissing)]
    [InlineData(ValidationCode.IcoInvalid)]
    [InlineData(ValidationCode.DicMissing)]
    [InlineData(ValidationCode.DicInvalid)]
    [InlineData(ValidationCode.RegisterEntryMissing)]
    [InlineData(ValidationCode.BankAccountInvalid)]
    [InlineData(ValidationCode.IssuedByMissing)]
    public void Validate_InvalidSupplier_ReportsExpectedCode(ValidationCode code)
    {
        var supplier = TestData.Supplier();
        SetInvalidSupplierField(supplier, code);
        Assert.Equal(new ValidationMessage(code, ValidationTarget.Supplier), Assert.Single(supplier.Validate()));
    }

    [Theory]
    [InlineData(ValidationCode.NameMissing)]
    [InlineData(ValidationCode.AddressIncomplete)]
    [InlineData(ValidationCode.IcoInvalid)]
    [InlineData(ValidationCode.DicInvalid)]
    [InlineData(ValidationCode.EmailInvalid)]
    public void Validate_InvalidCustomer_ReportsExpectedCode(ValidationCode code)
    {
        var customer = TestData.Customer();
        switch (code)
        {
            case ValidationCode.NameMissing: customer.Name = ""; break;
            case ValidationCode.AddressIncomplete: customer.Street = ""; break;
            case ValidationCode.IcoInvalid: customer.Ico = "bad"; break;
            case ValidationCode.DicInvalid: customer.Dic = "bad"; break;
            case ValidationCode.EmailInvalid: customer.Email = "bad"; break;
        }
        Assert.Equal(new ValidationMessage(code, ValidationTarget.Customer), Assert.Single(customer.Validate()));
    }

    private static void SetInvalidSupplierField(Supplier supplier, ValidationCode code)
    {
        switch (code)
        {
            case ValidationCode.NameMissing: supplier.Name = ""; break;
            case ValidationCode.AddressIncomplete: supplier.City = ""; break;
            case ValidationCode.IcoMissing: supplier.Ico = null; break;
            case ValidationCode.IcoInvalid: supplier.Ico = "bad"; break;
            case ValidationCode.DicMissing: supplier.Dic = ""; break;
            case ValidationCode.DicInvalid: supplier.Dic = "bad"; break;
            case ValidationCode.RegisterEntryMissing: supplier.RegisterEntry = ""; break;
            case ValidationCode.BankAccountInvalid: supplier.BankAccount = ""; break;
            case ValidationCode.IssuedByMissing: supplier.IssuedBy = ""; break;
        }
    }
}
