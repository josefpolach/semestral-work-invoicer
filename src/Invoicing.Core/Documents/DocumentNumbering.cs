using System.Globalization;

namespace Invoicing.Core.Documents;

/// <summary>Formats yearly document numbers and payment references.</summary>
public static class DocumentNumbering
{
    private const int MaxVariableSymbolLength = 10;

    public static NumberSeries GetSeries(DocumentKind kind) => kind switch
    {
        DocumentKind.TaxInvoice or DocumentKind.NonVatInvoice => NumberSeries.Invoice,
        DocumentKind.CreditNote => NumberSeries.CreditNote,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static int NextSequenceNumber(int? lastNumberInYear) => checked((lastNumberInYear ?? 0) + 1);

    public static string Format(NumberSeries series, int year, int sequence)
    {
        var number = string.Create(CultureInfo.InvariantCulture, $"{year:0000}-{sequence:0000}");
        return series switch
        {
            NumberSeries.Invoice => number,
            NumberSeries.CreditNote => "OD-" + number,
            _ => throw new ArgumentOutOfRangeException(nameof(series))
        };
    }

    public static string ToVariableSymbol(string invoiceNumber)
    {
        var symbol = invoiceNumber.Replace("-", "");
        return symbol.Length is > 0 and <= MaxVariableSymbolLength && symbol.All(char.IsAsciiDigit)
            ? symbol : throw new ArgumentException("Invalid invoice number.", nameof(invoiceNumber));
    }
}
