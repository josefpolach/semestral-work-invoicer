using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Repositories;
using Invoicing.Core.Services;
using Invoicing.Email;
using Invoicing.Pdf;

namespace Invoicing.App.Services;

/// <summary>Prepares the same invoice attachment for export and e-mail.</summary>
internal class DocumentDelivery(EmailSettings settings, DocumentPdfGenerator pdf, IDocumentEmailRepository records,
    DocumentEmailComposer composer, DocumentEmailService mail)
{
    private const string PdfType = "application/pdf";
    public EmailSettings Settings { get; } = settings;
    public DocumentEmailService Mail { get; } = mail;

    public Task<DocumentEmailRecord?> GetLatestAsync(Document document) => document.Id == 0
        ? Task.FromResult<DocumentEmailRecord?>(null) : records.GetLatestAsync(document.Id);

    public Task<EmailAttachment> PrepareAsync(Document document) =>
        Task.Run(() => new EmailAttachment(PdfFileNames.For(document), PdfType, pdf.Generate(document)));

    public EmailMessage Compose(Document document, EmailAttachment attachment) =>
        composer.Compose(document, document.Customer?.Email ?? string.Empty, Settings.FromAddress, attachment);
}
