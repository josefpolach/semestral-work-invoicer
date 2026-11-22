using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Invoicing.App.Texts;
using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;

namespace Invoicing.App.ViewModels;

/// <summary>Keeps numeric input as text while the user edits an invoice line.</summary>
public partial class InvoiceLineViewModel : ObservableObject
{
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _quantity;
    [ObservableProperty] private string _unit;
    [ObservableProperty] private string _unitPrice;
    [ObservableProperty] private string _discount;
    [ObservableProperty] private decimal _vatRate;

    public InvoiceLineViewModel(InvoiceLine line, IReadOnlyList<decimal> vatRates)
    {
        _description = line.Description;
        _quantity = Format(line.Quantity);
        _unit = line.Unit;
        _unitPrice = Format(line.UnitPriceWithoutVat);
        _discount = Format(line.DiscountPercent);
        _vatRate = line.VatRate;
        VatRates = vatRates.Contains(line.VatRate) ? vatRates : [.. vatRates, line.VatRate];
    }

    public IReadOnlyList<decimal> VatRates { get; }
    public bool IsValid => QuantityError is null && PriceError is null && DiscountError is null;
    public string Base => IsValid ? CzechFormat.FormatMoney(ToLine().CalculateBase()) : DocumentTexts.InvalidNumber;
    public string? QuantityError => Check(Quantity);
    public string? PriceError => Check(UnitPrice);
    public string? DiscountError => Check(Discount);

    public InvoiceLine ToLine() =>
        new(Description ?? string.Empty, Parse(Quantity), Unit ?? string.Empty, Parse(UnitPrice), VatRate, Parse(Discount));

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.PropertyName is nameof(Quantity) or nameof(UnitPrice) or nameof(Discount))
        {
            OnPropertyChanged(nameof(QuantityError));
            OnPropertyChanged(nameof(PriceError));
            OnPropertyChanged(nameof(DiscountError));
            OnPropertyChanged(nameof(Base));
        }
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture).Replace('.', ',');
    private static decimal Parse(string value) => CzechNumberParser.TryParseDecimal(value, out var number) ? number : 0;
    private static string? Check(string value) =>
        CzechNumberParser.TryParseDecimal(value, out _) ? null : DocumentTexts.InvalidNumber;
}
