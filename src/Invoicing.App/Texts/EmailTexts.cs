using Invoicing.Core.Configuration;
using Invoicing.Core.Email;
using Invoicing.Core.Formatting;

namespace Invoicing.App.Texts;

internal static class EmailTexts
{
    public static string Format(EmailSendResult result) => result.Error switch
    {
        EmailSendError.SenderDomainNotVerified => "Doména odesílatele není v Lettr ověřená.",
        EmailSendError.InvalidApiKey => "Neplatný API klíč Lettr.",
        EmailSendError.NotAllowed => "Pro tento klíč nebo doménu není odesílání povoleno.",
        EmailSendError.PreviousAttemptInProgress => "Předchozí pokus o odeslání se ještě zpracovává. Zkuste to za chvíli znovu.",
        EmailSendError.RateLimited => "Byl dosažen limit odesílání. Zkuste to později.",
        EmailSendError.ServiceUnavailable => "Služba Lettr je nedostupná. Zkuste to později.",
        EmailSendError.NoConnection => "Nepodařilo se spojit se službou Lettr. Zkontrolujte připojení k internetu.",
        EmailSendError.InvalidData => $"Lettr odmítl data e-mailu: {result.Details ?? "bez podrobností"}",
        EmailSendError.UnexpectedResponse => $"Neočekávaná odpověď služby Lettr (HTTP {result.StatusCode?.ToString() ?? "—"}).",
        _ => throw new ArgumentOutOfRangeException(nameof(result))
    };

    public static string? Availability(EmailUnavailableReason? reason) => reason switch
    {
        EmailUnavailableReason.NotConfigured => "Odesílání e-mailů není nastaveno. Doplňte LETTR_API_KEY a EMAIL_FROM_ADDRESS do souboru .env a restartujte aplikaci.",
        EmailUnavailableReason.DocumentIsDraft => "Doklad nejdříve vystavte.",
        EmailUnavailableReason.DocumentIsCancelled => "Stornovaný doklad nelze odeslat.",
        _ => null
    };

    public static string Status(EmailSettings settings) => settings.IsEnabled
        ? $"Odesílání e-mailů: zapnuto (odesílatel {settings.FromAddress})"
        : "Odesílání e-mailů: vypnuto – " + string.Join(", ", settings.Problems.Select(problem =>
            problem.Problem == ConfigurationProblem.Missing ? "chybí " + problem.Key : "neplatná hodnota " + problem.Key));

    public const string InvalidRecipient = "Zadejte platnou e-mailovou adresu.";
    public const string ReceiptSaveFailed = "Potvrzení o odeslání se nepodařilo uložit. Zkuste odeslání znovu v tomto okně.";

    public static string Sent(string? address) => $"E-mail byl odeslán na {address}.";
    public static string SentInfo(DocumentEmailRecord record) => $"Odesláno {FormatDate(record)} na {record.SentTo}";
    public static string PreviouslySent(DocumentEmailRecord record) =>
        $"Tento doklad už byl odeslán {FormatDate(record)} na {record.SentTo}.";

    private static string FormatDate(DocumentEmailRecord record) =>
        CzechFormat.FormatDate(DateOnly.FromDateTime(record.SentAt.LocalDateTime));
}
