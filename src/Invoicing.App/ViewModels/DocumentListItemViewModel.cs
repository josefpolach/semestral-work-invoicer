using Invoicing.App.Texts;
using Invoicing.Core.Documents;
using Invoicing.Core.Formatting;

namespace Invoicing.App.ViewModels;

/// <summary>Formats a document row in the list.</summary>
public class DocumentListItemViewModel(Document document, DateOnly today)
{
    public int Id { get; } = document.Id;
    public string Number { get; } = document.Number ?? DocumentTexts.Draft;
    public string Kind { get; } = DocumentTexts.Kind(document.Kind);
    public string Customer { get; } = document.Customer?.Name ?? string.Empty;
    public string IssueDate { get; } = DocumentTexts.Date(document.IssueDate);
    public string DueDate { get; } = DocumentTexts.Date(document.DueDate);
    public string Total { get; } = CzechFormat.FormatMoney(document.CalculateTotal());
    public string Status { get; } = DocumentTexts.Status(document, today);
}
