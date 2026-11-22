namespace Invoicing.Core.Email;

/// <summary>The provider's acceptance reference or the categorized reason sending failed.</summary>
public class EmailSendResult
{
    private EmailSendResult()
    {
    }

    public bool IsSuccess => ProviderRequestId is not null;
    public string? ProviderRequestId { get; private init; }
    public EmailSendError? Error { get; private init; }
    public string? Details { get; private init; }
    public int? StatusCode { get; private init; }

    public static EmailSendResult Success(string requestId) => new() { ProviderRequestId = requestId };
    public static EmailSendResult Failure(EmailSendError error, string? details = null, int? statusCode = null) =>
        new() { Error = error, Details = details, StatusCode = statusCode };
}
