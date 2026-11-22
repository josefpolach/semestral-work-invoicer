using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class EmailAvailabilityTests
{
    [Fact]
    public void Evaluate_UnconfiguredDraft_PrioritizesConfiguration()
    {
        var settings = new EmailSettings(null, null);
        Assert.Equal(EmailUnavailableReason.NotConfigured, EmailAvailability.Evaluate(settings, TestData.Invoice()).Reason);
    }

    [Theory]
    [InlineData(DocumentStatus.Draft, EmailUnavailableReason.DocumentIsDraft)]
    [InlineData(DocumentStatus.Cancelled, EmailUnavailableReason.DocumentIsCancelled)]
    [InlineData(DocumentStatus.Issued, null)]
    [InlineData(DocumentStatus.Paid, null)]
    public void Evaluate_ConfiguredEmail_UsesDocumentStatus(DocumentStatus status, EmailUnavailableReason? expected)
    {
        var settings = new EmailSettings("test", "from@example.com");
        var invoice = new NonVatInvoice(TestData.Header() with { Status = status }, TestData.Lines);
        var availability = EmailAvailability.Evaluate(settings, invoice);
        Assert.Equal(expected, availability.Reason);
        Assert.Equal(expected is null, availability.CanSend);
    }
}
