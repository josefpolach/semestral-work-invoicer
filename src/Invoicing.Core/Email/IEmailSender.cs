namespace Invoicing.Core.Email;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, string idempotencyKey, CancellationToken cancellationToken);
}
