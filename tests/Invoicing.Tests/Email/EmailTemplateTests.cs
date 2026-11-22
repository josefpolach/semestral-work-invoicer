using Invoicing.Core.Documents;
using Invoicing.Email;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Email;

public class EmailTemplateTests
{
    [Fact]
    public void Render_Placeholders_EncodeOnlyHtmlAndRejectUnknown()
    {
        var renderer = new EmailTemplateRenderer();
        var values = new Dictionary<string, string> { ["First"] = "<b>", ["Second"] = "&" };
        const string template = "{{First}} and {{Second}}";
        Assert.Equal("&lt;b&gt; and &amp;", renderer.Render(template, values, true));
        Assert.Equal("<b> and &", renderer.Render(template, values, false));
        Assert.Throws<TemplateRenderException>(() => renderer.Render("{{Unknown}}", values, false));
    }

    [Theory]
    [InlineData(DocumentKind.TaxInvoice, "Faktura 2026-0001 – ")]
    [InlineData(DocumentKind.NonVatInvoice, "Faktura 2026-0001 – ")]
    [InlineData(DocumentKind.CreditNote, "Opravný daňový doklad OD-2026-0001 – ")]
    public void Compose_EachDocumentKind_RendersEveryTemplate(DocumentKind kind, string subjectPrefix)
    {
        var composer = new DocumentEmailComposer(new(), new());
        var document = TestData.Issued(kind);
        var message = composer.Compose(document, "to@example.com", "from@example.com", new("test.pdf", "application/pdf", []));
        Assert.StartsWith(subjectPrefix, message.Subject);
        Assert.DoesNotContain("{{", message.Subject + message.HtmlBody + message.TextBody);
        if (kind == DocumentKind.TaxInvoice)
        {
            Assert.Contains("3\u00a0368,24\u00a0Kč", message.TextBody);
        }

        if (kind == DocumentKind.CreditNote)
        {
            Assert.DoesNotContain("Variabilní symbol", message.TextBody);
        }
    }
}
