using System.Net;

namespace Invoicing.Tests.TestHelpers;

internal class FakeHttpMessageHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
{
    public int Calls { get; private set; }
    public Uri? Url { get; private set; }
    public HttpMethod? Method { get; private set; }
    public string? Body { get; private set; }
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Url = request.RequestUri;
        Method = request.Method;
        foreach (var header in request.Headers)
        {
            Headers[header.Key] = string.Join(", ", header.Value);
        }

        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return respond();
    }

    public static HttpResponseMessage WithBody(HttpStatusCode status, string content) =>
        new(status) { Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json") };
}
