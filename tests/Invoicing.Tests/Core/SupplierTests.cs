using Invoicing.Core.Parties;
using Invoicing.Core.Validation;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class SupplierTests
{
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
