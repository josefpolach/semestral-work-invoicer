namespace Invoicing.Core.Documents;

/// <summary>VAT and totals grouped by rate.</summary>
public class VatSummary
{
    private VatSummary(IReadOnlyList<VatSummaryRow> rows) => Rows = rows;

    public IReadOnlyList<VatSummaryRow> Rows { get; }
    public decimal TotalBase => Rows.Sum(row => row.TaxBase);
    public decimal TotalVat => Rows.Sum(row => row.VatAmount);
    public decimal Total => TotalBase + TotalVat;

    public static VatSummary Calculate(IEnumerable<InvoiceLine> lines) => new(lines.GroupBy(line => line.VatRate)
        .OrderByDescending(group => group.Key).Select(CalculateRow).ToList().AsReadOnly());

    private static VatSummaryRow CalculateRow(IGrouping<decimal, InvoiceLine> group)
    {
        // Section 37 of the VAT Act: calculate VAT from the sum of bases for each rate.
        var taxBase = group.Sum(line => line.CalculateBase());
        return new(group.Key, taxBase, Math.Round(taxBase * group.Key / 100, 2, MidpointRounding.AwayFromZero));
    }
}
