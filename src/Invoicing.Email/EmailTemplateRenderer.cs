using System.Net;
using System.Text.RegularExpressions;

namespace Invoicing.Email;

/// <summary>Substitutes all template placeholders and encodes HTML values.</summary>
public class EmailTemplateRenderer
{
    private const string Placeholder = @"\{\{(\w+)\}\}";

    public string Render(string template, IReadOnlyDictionary<string, string> values, bool htmlEncodeValues) =>
        Regex.Replace(template, Placeholder, match =>
        {
            var name = match.Groups[1].Value;
            if (!values.TryGetValue(name, out var value))
            {
                throw new TemplateRenderException(name);
            }

            return htmlEncodeValues ? WebUtility.HtmlEncode(value) : value;
        });
}
