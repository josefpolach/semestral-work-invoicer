using Invoicing.Core.Validation;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class CustomerTests
{
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
}
