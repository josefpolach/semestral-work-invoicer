using System.Globalization;
using Invoicing.Core.Documents;
using Invoicing.Data.Rows;

namespace Invoicing.Data.Mappings;

internal static class DocumentLineMapper
{
    public static InvoiceLine Read(DocumentLineRow row) => new(row.Description, Parse(row.Quantity), row.Unit,
        Parse(row.UnitPrice), Parse(row.VatRate), Parse(row.DiscountPercent));

    public static DocumentLineRow Write(int id, int position, InvoiceLine line) => new()
    {
        DocumentId = id,
        Position = position,
        Description = line.Description,
        Unit = line.Unit,
        Quantity = Format(line.Quantity),
        UnitPrice = Format(line.UnitPriceWithoutVat),
        VatRate = Format(line.VatRate),
        DiscountPercent = Format(line.DiscountPercent)
    };

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
