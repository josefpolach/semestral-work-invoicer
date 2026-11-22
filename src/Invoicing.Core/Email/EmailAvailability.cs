using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;

namespace Invoicing.Core.Email;

/// <summary>Evaluates document e-mail prerequisites in user-facing priority order.</summary>
public class EmailAvailability
{
    private EmailAvailability(EmailUnavailableReason? reason) => Reason = reason;

    public bool CanSend => Reason is null;
    public EmailUnavailableReason? Reason { get; }

    public static EmailAvailability Evaluate(EmailSettings settings, Document document)
    {
        if (!settings.IsEnabled)
        {
            return new(EmailUnavailableReason.NotConfigured);
        }

        if (document.Status == DocumentStatus.Draft)
        {
            return new(EmailUnavailableReason.DocumentIsDraft);
        }

        if (document.Status == DocumentStatus.Cancelled)
        {
            return new(EmailUnavailableReason.DocumentIsCancelled);
        }

        return new(null);
    }
}
