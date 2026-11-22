namespace Invoicing.Data.Rows;

internal class DocumentLineRow
{
    public int DocumentId { get; set; }
    public int Position { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string UnitPrice { get; set; } = string.Empty;
    public string VatRate { get; set; } = string.Empty;
    public string DiscountPercent { get; set; } = string.Empty;
}
