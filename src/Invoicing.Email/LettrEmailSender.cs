using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Invoicing.Core.Email;

namespace Invoicing.Email;

/// <summary>Submits a transactional e-mail to Lettr.</summary>
public class LettrEmailSender(HttpClient httpClient, string baseUrl, string apiKey) : IEmailSender
{
    private const string SendPath = "/emails";
    private const string JsonMediaType = "application/json";
    private const string IdempotencyHeader = "Idempotency-Key";

    public async Task<EmailSendResult> SendAsync(EmailMessage message, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Any(char.IsControl))
        {
            return EmailSendResult.Failure(EmailSendError.InvalidApiKey);
        }

        try
        {
            using var request = CreateRequest(message, idempotencyKey);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return await ReadResponseAsync(response, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EmailSendResult.Failure(EmailSendError.NoConnection);
        }
        catch (HttpRequestException)
        {
            return EmailSendResult.Failure(EmailSendError.NoConnection);
        }
    }

    private HttpRequestMessage CreateRequest(EmailMessage message, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + SendPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        request.Headers.Add(IdempotencyHeader, idempotencyKey);
        request.Content = JsonContent.Create(new LettrRequest(message));
        return request;
    }

    private async Task<EmailSendResult> ReadResponseAsync(HttpResponseMessage response, CancellationToken token)
    {
        var status = (int)response.StatusCode;
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var requestId = await LettrResponse.ReadIdAsync(response.Content, token);
            return string.IsNullOrWhiteSpace(requestId) || requestId.Contains(apiKey, StringComparison.Ordinal)
                ? EmailSendResult.Failure(EmailSendError.UnexpectedResponse, statusCode: status)
                : EmailSendResult.Success(requestId);
        }

        var error = MapStatus(status);
        var details = error == EmailSendError.InvalidData
            ? await LettrResponse.ReadDetailsAsync(response.Content, token) : null;
        return EmailSendResult.Failure(error, details?.Replace(apiKey, "[redacted]", StringComparison.Ordinal), status);
    }

    private static EmailSendError MapStatus(int status) => status switch
    {
        400 => EmailSendError.SenderDomainNotVerified,
        401 => EmailSendError.InvalidApiKey,
        403 => EmailSendError.NotAllowed,
        409 => EmailSendError.PreviousAttemptInProgress,
        422 => EmailSendError.InvalidData,
        429 => EmailSendError.RateLimited,
        >= 500 and <= 599 => EmailSendError.ServiceUnavailable,
        _ => EmailSendError.UnexpectedResponse
    };
}
