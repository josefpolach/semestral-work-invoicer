using Invoicing.App.Texts;
using Invoicing.App.ViewModels;
using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Repositories;
using Invoicing.Core.Services;
using Invoicing.Email;
using Invoicing.Pdf;

namespace Invoicing.App.Services;

/// <summary>Prepares and delivers the same invoice PDF for export and e-mail.</summary>
internal class DocumentDelivery(EmailSettings settings, DocumentPdfGenerator pdf, IDocumentEmailRepository records,
    IEmailSender sender)
{
    private const string PdfType = "application/pdf";
    private readonly DocumentEmailComposer _composer = new(new EmailTemplateStore(), new EmailTemplateRenderer());
    private readonly DocumentEmailService _mail = new(sender, records);

    public EmailAvailability GetAvailability(Document document) => EmailAvailability.Evaluate(settings, document);

    public Task<DocumentEmailRecord?> GetLatestAsync(Document document) => document.Id == 0
        ? Task.FromResult<DocumentEmailRecord?>(null) : records.GetLatestAsync(document.Id);

    public async Task ExportAsync(Document document, IDialogService dialogs)
    {
        var attachment = await PrepareAsync(document);
        var message = await SaveFileAsync(attachment, dialogs);
        if (message is not null)
        {
            await dialogs.ShowMessageAsync(message);
        }
    }

    public async Task<bool> SendAsync(Document document, DocumentEmailRecord? latest, IDialogService dialogs)
    {
        var attachment = await PrepareAsync(document);
        var message = _composer.Compose(document, document.Customer?.Email ?? string.Empty, settings.FromAddress, attachment);
        var model = new SendEmailViewModel(document, message, _mail, latest);
        if (!await dialogs.ShowEmailAsync(model))
        {
            return false;
        }

        await dialogs.ShowMessageAsync(EmailTexts.Sent(model.Recipient?.Trim()));
        return true;
    }

    private Task<EmailAttachment> PrepareAsync(Document document) =>
        Task.Run(() => new EmailAttachment(PdfFileNames.For(document), PdfType, pdf.Generate(document)));

    private static async Task<string?> SaveFileAsync(EmailAttachment pdfFile, IDialogService dialogs)
    {
        try
        {
            await using var stream = await dialogs.PickPdfAsync(pdfFile.FileName);
            if (stream is null)
            {
                return null;
            }

            await WriteFileAsync(stream, pdfFile.Content);
            return UiTexts.PdfSaved;
        }
        catch (IOException)
        {
            return UiTexts.PdfSaveFailed;
        }
        catch (UnauthorizedAccessException)
        {
            return UiTexts.PdfWriteDenied;
        }
    }

    private static async Task WriteFileAsync(Stream stream, byte[] bytes)
    {
        if (stream.CanSeek)
        {
            stream.SetLength(0);
        }

        await stream.WriteAsync(bytes);
    }
}
