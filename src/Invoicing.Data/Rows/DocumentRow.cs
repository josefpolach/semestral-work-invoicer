namespace Invoicing.Data.Rows;

internal class DocumentRow
{
    public int Id { get; set; }
    public string? OriginalNumber { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IssueDate { get; set; }
    public string? TaxableSupplyDate { get; set; }
    public string? DueDate { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerStreet { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerPostalCode { get; set; }
    public string? CustomerIco { get; set; }
    public string? CustomerDic { get; set; }
    public string? CustomerEmail { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierStreet { get; set; }
    public string? SupplierCity { get; set; }
    public string? SupplierPostalCode { get; set; }
    public string? SupplierIco { get; set; }
    public string? SupplierDic { get; set; }
    public string? SupplierRegisterEntry { get; set; }
    public string? SupplierBankAccount { get; set; }
    public string? SupplierIban { get; set; }
    public string? SupplierIssuedBy { get; set; }
    public int? OriginalDocumentId { get; set; }
    public string? CorrectionReason { get; set; }
}
