using System.Net;
using System.Text.Json;
using Invoicing.Core.Email;
using Invoicing.Email;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Email;

public class LettrEmailSenderTests
{
    private const string BaseUrl = "https://example.com/api";
    private const string Key = "test-key";
    private const string RequestKey = "send-request";

    [Fact]
    public async Task SendAsync_Accepted_UsesProviderContract()
    {
        using var handler = new FakeHttpMessageHandler(() =>
            FakeHttpMessageHandler.WithBody(HttpStatusCode.OK, """{"data":{"request_id":"message-id"}}"""));
        using var client = new HttpClient(handler);
        var message = CreateMessage();
        var result = await new LettrEmailSender(client, BaseUrl, Key).SendAsync(message, RequestKey, CancellationToken.None);
        Assert.Equal("message-id", result.ProviderRequestId);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(BaseUrl + "/emails", handler.Url?.AbsoluteUri);
        Assert.Equal("Bearer " + Key, handler.Headers["Authorization"]);
        Assert.Equal("application/json", handler.Headers["Accept"]);
        Assert.Equal(RequestKey, handler.Headers["Idempotency-Key"]);
        using var json = JsonDocument.Parse(Assert.IsType<string>(handler.Body));
        AssertMessage(json.RootElement, message);
    }

    [Theory]
    [InlineData(400, EmailSendError.SenderDomainNotVerified)]
    [InlineData(401, EmailSendError.InvalidApiKey)]
    [InlineData(403, EmailSendError.NotAllowed)]
    [InlineData(409, EmailSendError.PreviousAttemptInProgress)]
    [InlineData(422, EmailSendError.InvalidData)]
    [InlineData(429, EmailSendError.RateLimited)]
    [InlineData(500, EmailSendError.ServiceUnavailable)]
    [InlineData(503, EmailSendError.ServiceUnavailable)]
    [InlineData(404, EmailSendError.UnexpectedResponse)]
    [InlineData(200, EmailSendError.UnexpectedResponse)]
    public async Task SendAsync_FailureResponse_MapsStatusAndRedactsKey(int status, EmailSendError expected)
    {
        const string body = """{"message":"Invalid test-key","errors":{"from":["Bad domain"]}}""";
        using var handler = new FakeHttpMessageHandler(() => FakeHttpMessageHandler.WithBody((HttpStatusCode)status, body));
        using var client = new HttpClient(handler);
        var sender = new LettrEmailSender(client, BaseUrl, Key);
        var result = await sender.SendAsync(CreateMessage(), RequestKey, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.Error);
        Assert.Equal(status, result.StatusCode);
        if (status == 422)
        {
            Assert.Equal("Invalid [redacted] from: Bad domain", result.Details);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_TimeoutOrNetworkError_ReturnsNoConnection(bool timeout)
    {
        using var handler = new FakeHttpMessageHandler(() =>
            throw (timeout ? new TaskCanceledException() : new HttpRequestException()));
        using var client = new HttpClient(handler);
        var result = await new LettrEmailSender(client, BaseUrl, Key)
            .SendAsync(CreateMessage(), RequestKey, CancellationToken.None);
        Assert.Equal(EmailSendError.NoConnection, result.Error);
    }

    internal static EmailMessage CreateMessage() => new("from@example.com", "Sender", "to@example.com", "Invoice",
        "<p>Invoice</p>", "Invoice", new("test.pdf", "application/pdf", [1, 2, 3, 255]));

    private static void AssertMessage(JsonElement json, EmailMessage message)
    {
        Assert.Equal(message.FromAddress, json.GetProperty("from").GetString());
        Assert.Equal(message.FromName, json.GetProperty("from_name").GetString());
        Assert.Equal(message.ToAddress, Assert.Single(json.GetProperty("to").EnumerateArray()).GetString());
        Assert.Equal(message.Subject, json.GetProperty("subject").GetString());
        Assert.Equal(message.HtmlBody, json.GetProperty("html").GetString());
        Assert.Equal(message.TextBody, json.GetProperty("text").GetString());
        var attachment = Assert.Single(json.GetProperty("attachments").EnumerateArray());
        Assert.Equal(message.Attachment.FileName, attachment.GetProperty("name").GetString());
        Assert.Equal(message.Attachment.ContentType, attachment.GetProperty("type").GetString());
        var data = Assert.IsType<string>(attachment.GetProperty("data").GetString());
        Assert.DoesNotContain("\n", data);
        Assert.Equal(message.Attachment.Content, Convert.FromBase64String(data));
    }
}
