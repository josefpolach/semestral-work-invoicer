namespace Invoicing.Core.Documents;

/// <summary>An immutable line of a billing document.</summary>
public class InvoiceLine(string description, decimal quantity, string unit,
    decimal unitPriceWithoutVat, decimal vatRate, decimal discountPercent)
{
    public string Description { get; } = description;
    public decimal Quantity { get; } = quantity;
    public string Unit { get; } = unit;
    public decimal UnitPriceWithoutVat { get; } = unitPriceWithoutVat;
    public decimal VatRate { get; } = vatRate;
    public decimal DiscountPercent { get; } = discountPercent;

    public decimal CalculateBase() =>
        Math.Round(Quantity * UnitPriceWithoutVat * (1 - DiscountPercent / 100), 2, MidpointRounding.AwayFromZero);

    public InvoiceLine WithNegatedQuantity() =>
        new(Description, -Quantity, Unit, UnitPriceWithoutVat, VatRate, DiscountPercent);

    public InvoiceLine WithoutVat() => new(Description, Quantity, Unit, UnitPriceWithoutVat, 0, DiscountPercent);
}
