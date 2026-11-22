namespace Invoicing.Core.Documents;

/// <summary>Combined base and tax for one VAT rate.</summary>
public record VatSummaryRow(decimal VatRate, decimal TaxBase, decimal VatAmount)
{
    public decimal Total => TaxBase + VatAmount;
}
