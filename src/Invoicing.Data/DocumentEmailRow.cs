namespace Invoicing.Data;

internal class DocumentEmailRow
{
    public string SentTo { get; set; } = string.Empty;
    public string SentAt { get; set; } = string.Empty;
    public string ProviderRequestId { get; set; } = string.Empty;
}
