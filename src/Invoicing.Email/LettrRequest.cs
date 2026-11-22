using System.Text.Json.Serialization;
using Invoicing.Core.Email;

namespace Invoicing.Email;

internal class LettrRequest(EmailMessage message)
{
    [JsonPropertyName("from")]
    public string From { get; } = message.FromAddress;
    [JsonPropertyName("from_name")]
    public string FromName { get; } = message.FromName;
    [JsonPropertyName("to")]
    public string[] To { get; } = [message.ToAddress];
    [JsonPropertyName("subject")]
    public string Subject { get; } = message.Subject;
    [JsonPropertyName("html")]
    public string Html { get; } = message.HtmlBody;
    [JsonPropertyName("text")]
    public string Text { get; } = message.TextBody;
    [JsonPropertyName("attachments")]
    public LettrAttachment[] Attachments { get; } = [new(message.Attachment)];
}
