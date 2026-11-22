using Invoicing.Email;

namespace Invoicing.Tests.Email;

public class EmailTemplateRendererTests
{
    [Theory]
    [InlineData(false, "<b> and &")]
    [InlineData(true, "&lt;b&gt; and &amp;")]
    public void Render_KnownPlaceholders_EncodesOnlyHtml(bool encode, string expected)
    {
        var values = new Dictionary<string, string> { ["First"] = "<b>", ["Second"] = "&" };
        Assert.Equal(expected, new EmailTemplateRenderer().Render("{{First}} and {{Second}}", values, encode));
    }

    [Fact]
    public void Render_UnknownPlaceholder_ReportsTemplateError()
    {
        var exception = Assert.Throws<TemplateRenderException>(() =>
            new EmailTemplateRenderer().Render("{{Unknown}}", new Dictionary<string, string>(), false));
        Assert.Contains("Unknown", exception.Message);
    }
}
