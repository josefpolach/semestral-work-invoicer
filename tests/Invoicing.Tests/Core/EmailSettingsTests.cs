using Invoicing.Core.Configuration;

namespace Invoicing.Tests.Core;

public class EmailSettingsTests
{
    [Fact]
    public void ToString_ConfiguredEmail_OmitsApiKey()
    {
        var settings = new EmailSettings("test-secret", "from@example.com");
        Assert.DoesNotContain("test-secret", settings.ToString());
    }
}
