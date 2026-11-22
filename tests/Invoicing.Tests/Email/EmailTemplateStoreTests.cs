using Invoicing.Core.Documents;
using Invoicing.Email;

namespace Invoicing.Tests.Email;

public class EmailTemplateStoreTests
{
    [Theory]
    [InlineData(DocumentKind.TaxInvoice, EmailTemplateStore.SubjectExtension)]
    [InlineData(DocumentKind.TaxInvoice, EmailTemplateStore.HtmlExtension)]
    [InlineData(DocumentKind.TaxInvoice, EmailTemplateStore.TextExtension)]
    [InlineData(DocumentKind.CreditNote, EmailTemplateStore.SubjectExtension)]
    [InlineData(DocumentKind.CreditNote, EmailTemplateStore.HtmlExtension)]
    [InlineData(DocumentKind.CreditNote, EmailTemplateStore.TextExtension)]
    public void Load_EachSupportedTemplate_ReadsNonEmptyResource(DocumentKind kind, string extension) =>
        Assert.Contains("{{DocumentNumber}}", new EmailTemplateStore().Load(kind, extension));

    [Fact]
    public void Load_UnknownTemplate_ReportsMissingResource() =>
        Assert.Throws<InvalidOperationException>(() => new EmailTemplateStore().Load(DocumentKind.TaxInvoice, "invalid"));
}
