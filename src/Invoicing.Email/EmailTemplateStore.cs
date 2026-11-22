using Invoicing.Core.Documents;

namespace Invoicing.Email;

/// <summary>Reads the embedded invoice and credit note templates.</summary>
public class EmailTemplateStore
{
    public const string SubjectExtension = "subject.txt";
    public const string HtmlExtension = "html";
    public const string TextExtension = "txt";

    private const string ResourcePrefix = "Invoicing.Email.Templates.";

    public string Load(DocumentKind kind, string extension)
    {
        var name = kind == DocumentKind.CreditNote ? "credit-note" : "invoice";
        var resource = ResourcePrefix + name + "." + extension;
        using var stream = typeof(EmailTemplateStore).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing template: {resource}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
