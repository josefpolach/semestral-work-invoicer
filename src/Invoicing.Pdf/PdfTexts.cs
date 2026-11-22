using Invoicing.Core.Documents;

namespace Invoicing.Pdf;

internal static class PdfTexts
{
    public const string Supplier = "Dodavatel";
    public const string Customer = "Odběratel";
    public const string CompanyNumber = "IČO";
    public const string TaxNumber = "DIČ";
    public const string IssuedBy = "Vystavil";
    public const string Page = "Strana ";
    public const string Cancelled = "STORNOVÁNO";
    public const string IssueDate = "Datum vystavení";
    public const string SupplyDate = "Datum uskutečnění zdanitelného plnění";
    public const string DueDate = "Datum splatnosti";
    public const string Payment = "Forma úhrady: bankovním převodem";
    public const string Account = "Číslo účtu";
    public const string Iban = "IBAN";
    public const string VariableSymbol = "Variabilní symbol";
    public const string Description = "Popis";
    public const string Quantity = "Množství";
    public const string Unit = "Jednotka";
    public const string UnitPrice = "Cena za jednotku";
    public const string NetUnitPrice = "Cena za jednotku bez DPH";
    public const string Discount = "Sleva %";
    public const string VatRate = "Sazba DPH";
    public const string Base = "Základ";
    public const string Total = "Celkem";
    public const string Rate = "Sazba";
    public const string TaxBase = "Základ daně";
    public const string Vat = "DPH";
    public const string VatSummary = "Rekapitulace DPH";
    public const string CorrectionSummary = "Rozdíl základu daně a DPH podle sazeb";
    public const string Original = "Opravovaný doklad";
    public const string Reason = "Důvod opravy";
    public const string TotalToPay = "Celkem k úhradě";
    public const string CorrectionTotal = "Celková částka opravy";
    public const string NonPayer = "Nejsem plátce DPH.";
    public const string QrPayment = "QR Platba";
    public const string QrTooLarge = "QR Platbu nelze vytvořit, částka je příliš vysoká.";
    public const string Invoice = "Faktura";
    public const string Number = "č.";

    public static string Title(DocumentKind kind) => kind switch
    {
        DocumentKind.TaxInvoice => "Faktura – daňový doklad",
        DocumentKind.NonVatInvoice => Invoice,
        DocumentKind.CreditNote => "Opravný daňový doklad",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
