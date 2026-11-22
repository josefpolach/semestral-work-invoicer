namespace Invoicing.Core.Configuration;

/// <summary>The two VAT rates accepted for new tax documents.</summary>
public class VatRates(decimal standard, decimal reduced)
{
    public decimal Standard { get; } = standard;
    public decimal Reduced { get; } = reduced;
    public IReadOnlyList<decimal> All { get; } =
        Array.AsReadOnly(new[] { standard, reduced }.Distinct().OrderByDescending(rate => rate).ToArray());

    public bool IsAllowed(decimal rate) => All.Contains(rate);
}
