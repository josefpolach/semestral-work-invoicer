namespace Invoicing.Core.Documents;

/// <summary>VAT and totals grouped by rate.</summary>
public class VatSummary
{
    private VatSummary(IReadOnlyList<VatSummaryRow> rows) => Rows = rows;

    public IReadOnlyList<VatSummaryRow> Rows { get; }
    public decimal TotalBase => Rows.Sum(row => row.TaxBase);
    public decimal TotalVat => Rows.Sum(row => row.VatAmount);
    public decimal Total => TotalBase + TotalVat;

    public static VatSummary Calculate(IEnumerable<InvoiceLine> lines)
    {
        var rows = new List<VatSummaryRow>();
        var groups = lines.GroupBy(line => line.VatRate).OrderByDescending(group => group.Key);
        foreach (var group in groups)
        {
            // Section 37 of the VAT Act: calculate VAT from the sum of bases for each rate.
            var taxBase = group.Sum(line => line.CalculateBase());
            var vat = Math.Round(taxBase * group.Key / 100, 2, MidpointRounding.AwayFromZero);
            rows.Add(new(group.Key, taxBase, vat));
        }

        return new(rows.AsReadOnly());
    }
}
