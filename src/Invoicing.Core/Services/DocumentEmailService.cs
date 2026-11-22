using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Repositories;

namespace Invoicing.Core.Services;

/// <summary>Records delivery attempts accepted by the e-mail provider.</summary>
public class DocumentEmailService(IEmailSender sender, IDocumentEmailRepository emails)
{
    public async Task<EmailSendResult> SendAsync(Document document, EmailMessage message, string idempotencyKey,
        DateTimeOffset sentAt, CancellationToken cancellationToken)
    {
        if (document.Id <= 0 || document.Status is not (DocumentStatus.Issued or DocumentStatus.Paid))
        {
            throw new InvalidOperationException("Only a persisted active document can be e-mailed.");
        }

        var result = await sender.SendAsync(message, idempotencyKey, cancellationToken);
        if (result.ProviderRequestId is string requestId)
        {
            await emails.AddAsync(document.Id, new(message.ToAddress, sentAt, requestId));
        }

        return result;
    }
}
