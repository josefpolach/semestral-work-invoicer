using Invoicing.Core.Validation;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class TaxInvoiceValidationTests
{
    [Fact]
    public void Validate_CompleteDraft_ReturnsNoIssues()
    {
        var draft = TestData.Invoice();
        Assert.Empty(draft.Validate(TestData.Supplier(), TestData.Rates));
    }

    [Theory]
    [InlineData(true, ValidationTarget.Supplier)]
    [InlineData(false, ValidationTarget.Customer)]
    public void Validate_MissingParty_ReportsExpectedTarget(bool supplierMissing, ValidationTarget target)
    {
        var draft = TestData.Invoice();
        if (!supplierMissing)
        {
            draft.Customer = null;
        }

        var supplier = supplierMissing ? null : TestData.Supplier();
        Assert.Equal(new ValidationMessage(ValidationCode.Missing, target), Assert.Single(draft.Validate(supplier, TestData.Rates)));
    }

    [Fact]
    public void Validate_NonVatSupplier_RequiresVatPayer()
    {
        var draft = TestData.Invoice();
        Assert.Equal(new ValidationMessage(ValidationCode.MustBeVatPayer, ValidationTarget.Supplier),
            Assert.Single(draft.Validate(TestData.Supplier(false), TestData.Rates)));
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
}
