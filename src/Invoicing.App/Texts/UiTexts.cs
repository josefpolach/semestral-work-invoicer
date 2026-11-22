namespace Invoicing.App.Texts;

internal static class UiTexts
{
    public const string ExportPdf = "Export PDF";
    public const string PdfSaved = "PDF bylo uloženo.";
    public const string PdfSaveFailed = "PDF se nepodařilo uložit.";
    public const string PdfWriteDenied = "Do vybraného souboru nelze zapisovat.";
    public const string ConfirmIssue = "Vystavit doklad? Po vystavení už nepůjde upravit.";
    public const string ConfirmDelete = "Opravdu chcete smazat tento koncept?";
    public const string ConfirmCancel = "Opravdu chcete doklad stornovat? Tuto akci nelze vrátit.";
    public const string DocumentMissing = "Doklad již v databázi není.";
    public const string DatabaseWriteFailed = "Operaci se nepodařilo uložit do databáze. Zkuste to znovu.";
    public const string InvalidBankAccount = "Neplatné číslo účtu";
    public const string SupplierSaved = "Údaje dodavatele byly uloženy.";
    public const string DefaultLineUnit = "ks";

    public static string UnexpectedError(string message) => $"Nastala neočekávaná chyba: {message}";
}
