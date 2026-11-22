using Invoicing.Core.Validation;

namespace Invoicing.App.Texts;

internal static class ValidationTexts
{
    public static string Format(ValidationMessage message)
    {
        var text = GetText(message.Code);
        return message.Target switch
        {
            ValidationTarget.Supplier => $"Dodavatel: {text}.",
            ValidationTarget.Customer => $"Odběratel: {text}.",
            ValidationTarget.Line => $"Řádek {message.LinePosition}: {text}.",
            _ => text
        };
    }

    private static string GetText(ValidationCode code) => code switch
    {
        ValidationCode.Missing => "není vyplněn",
        ValidationCode.NameMissing => "chybí název nebo jméno",
        ValidationCode.AddressIncomplete => "adresa není úplná (ulice, obec, PSČ)",
        ValidationCode.IcoMissing => "chybí IČO",
        ValidationCode.IcoInvalid => "IČO není platné",
        ValidationCode.DicMissing => "chybí DIČ",
        ValidationCode.DicInvalid => "DIČ má neplatný formát (např. CZ12345678)",
        ValidationCode.RegisterEntryMissing => "chybí zápis v obchodním rejstříku",
        ValidationCode.BankAccountInvalid => "chybí číslo účtu nebo je neplatné",
        ValidationCode.IssuedByMissing => "chybí údaj Vystavil",
        ValidationCode.EmailInvalid => "e-mail není platný",
        ValidationCode.MustBeVatPayer => "musí být plátce DPH",
        ValidationCode.MustNotBeVatPayer => "nesmí být plátce DPH",
        ValidationCode.DescriptionMissing => "chybí popis",
        ValidationCode.QuantityNotPositive => "množství musí být větší než nula",
        ValidationCode.QuantityZero => "množství nesmí být nula",
        ValidationCode.UnitPriceNegative => "cena za jednotku nesmí být záporná",
        ValidationCode.VatRateNotAllowed => "sazba DPH není povolená",
        ValidationCode.DiscountOutOfRange => "sleva musí být od 0 do 100 %",
        ValidationCode.NoLines => "Doklad musí mít alespoň jeden řádek.",
        ValidationCode.IssueDateMissing => "Chybí datum vystavení.",
        ValidationCode.DueDateMissing => "Chybí datum splatnosti.",
        ValidationCode.TaxableSupplyDateMissing => "Chybí datum uskutečnění zdanitelného plnění (DUZP).",
        ValidationCode.DueDateBeforeIssueDate => "Datum splatnosti nesmí být dříve než datum vystavení.",
        ValidationCode.IssuedLongAfterTaxableSupply => "Upozornění: doklad je vystaven více než 15 dní po DUZP (§ 28 zákona o DPH).",
        ValidationCode.CorrectionReasonMissing => "Chybí důvod opravy.",
        ValidationCode.OriginalInvoiceMissing => "Chybí opravovaná faktura.",
        _ => throw new ArgumentOutOfRangeException(nameof(code))
    };
}
