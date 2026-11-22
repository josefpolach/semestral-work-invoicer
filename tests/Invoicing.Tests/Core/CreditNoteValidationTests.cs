using Invoicing.Core.Documents;
using Invoicing.Core.Validation;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class CreditNoteValidationTests
{
    [Fact]
    public void Validate_NegativeQuantity_IsAllowed()
    {
        var credit = CreateDraft(-1);
        Assert.Empty(credit.Validate(TestData.Supplier(), TestData.Rates));
    }

    [Fact]
    public void Validate_TaxableSupplyMoreThanFifteenDaysAgo_ReturnsNoWarning()
    {
        var credit = CreateDraft(-1);
        credit.TaxableSupplyDate = TestData.Today.AddDays(-20);
        Assert.Empty(credit.Validate(TestData.Supplier(), TestData.Rates));
    }

    [Fact]
    public void Validate_ZeroQuantity_ReportsLinePosition()
    {
        var credit = CreateDraft(0);
        Assert.Equal(new ValidationMessage(ValidationCode.QuantityZero, ValidationTarget.Line, LinePosition: 1),
            Assert.Single(credit.Validate(TestData.Supplier(), TestData.Rates)));
    }

    [Theory]
    [InlineData(ValidationCode.OriginalInvoiceMissing)]
    [InlineData(ValidationCode.CorrectionReasonMissing)]
    public void Validate_MissingCorrectionDetails_ReportsExpectedCode(ValidationCode code)
    {
        var credit = CreateDraft(-1);
        if (code == ValidationCode.OriginalInvoiceMissing)
        {
            credit.OriginalInvoice = null;
        }
        else
        {
            credit.CorrectionReason = string.Empty;
        }

        Assert.Equal(new ValidationMessage(code, ValidationTarget.Document),
            Assert.Single(credit.Validate(TestData.Supplier(), TestData.Rates)));
    }

    private static CreditNote CreateDraft(decimal quantity) =>
        new(TestData.Header(), [new("Item", quantity, "pc", 1, 21, 0)], TestData.Today, new(1, "2026-0001"))
        {
            CorrectionReason = "Quantity correction"
        };
}
