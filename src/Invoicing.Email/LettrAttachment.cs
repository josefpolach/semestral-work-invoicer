using System.Text.Json.Serialization;
using Invoicing.Core.Email;

namespace Invoicing.Email;

internal class LettrAttachment(EmailAttachment attachment)
{
    [JsonPropertyName("name")]
    public string Name { get; } = attachment.FileName;
    [JsonPropertyName("type")]
    public string Type { get; } = attachment.ContentType;
    [JsonPropertyName("data")]
    public string Data { get; } = Convert.ToBase64String(attachment.Content);
}
