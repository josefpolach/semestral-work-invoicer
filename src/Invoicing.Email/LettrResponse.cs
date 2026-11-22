using System.Text.Json;

namespace Invoicing.Email;

internal static class LettrResponse
{
    public static async Task<string?> ReadIdAsync(HttpContent content, CancellationToken token)
    {
        using var json = await ReadJsonAsync(content, token);
        return json?.RootElement is { ValueKind: JsonValueKind.Object } root &&
            root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
            ? GetText(data, "request_id") : null;
    }

    public static async Task<string?> ReadDetailsAsync(HttpContent content, CancellationToken token)
    {
        using var json = await ReadJsonAsync(content, token);
        if (json?.RootElement is not { ValueKind: JsonValueKind.Object } root)
        {
            return null;
        }

        var details = new List<string>();
        if (GetText(root, "message") is string message)
        {
            details.Add(message);
        }

        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            details.AddRange(errors.EnumerateObject().Select(error => $"{error.Name}: {Format(error.Value)}"));
        }

        return details.Count == 0 ? null : string.Join(" ", details);
    }

    private static string Format(JsonElement element) => element.ValueKind == JsonValueKind.Array
        ? string.Join(", ", element.EnumerateArray().Select(item => item.ToString())) : element.ToString();

    private static string? GetText(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static async Task<JsonDocument?> ReadJsonAsync(HttpContent content, CancellationToken token)
    {
        var body = await content.ReadAsStringAsync(token);
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
