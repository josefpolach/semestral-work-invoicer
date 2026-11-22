using Invoicing.Core.Documents;
using Invoicing.Core.Validation;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class NonVatInvoiceValidationTests
{
    [Fact]
    public void Validate_CompleteDraft_RequiresNeitherSupplyDateNorVatRate()
    {
        var draft = new NonVatInvoice(TestData.Header(), [new("Item", 1, "pc", 100m, 47m, 0)]);
        var result = draft.Validate(TestData.Supplier(false), TestData.Rates);
        Assert.Empty(result);
    }

    [Fact]
    public void ReplaceLines_EnteredVatRate_StoresZero()
    {
        var draft = new NonVatInvoice(TestData.Header(), []);
        draft.ReplaceLines([TestData.Lines[0]]);
        Assert.Equal(0m, Assert.Single(draft.Lines).VatRate);
    }

    [Fact]
    public void Validate_VatSupplier_RequiresNonVatPayer()
    {
        var draft = new NonVatInvoice(TestData.Header(), TestData.Lines);
        Assert.Equal(new ValidationMessage(ValidationCode.MustNotBeVatPayer, ValidationTarget.Supplier),
            Assert.Single(draft.Validate(TestData.Supplier(), TestData.Rates)));
    }
}
