namespace Invoicing.Core.Email;

public enum EmailSendError
{
    SenderDomainNotVerified, InvalidApiKey, NotAllowed, PreviousAttemptInProgress, InvalidData,
    RateLimited, ServiceUnavailable, NoConnection, UnexpectedResponse
}
