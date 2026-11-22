using Invoicing.Core.Parties;

namespace Invoicing.Core.Documents;

/// <summary>Common persisted state for the three types of documents.</summary>
public record DocumentHeader
{
    public int Id { get; init; }
    public string? Number { get; init; }
    public DocumentStatus Status { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public Customer? Customer { get; init; }
    public Supplier? Supplier { get; init; }

    public static DocumentHeader NewDraft(Customer? customer, DateOnly issueDate, DateOnly dueDate) =>
        new() { Customer = customer, IssueDate = issueDate, DueDate = dueDate, Status = DocumentStatus.Draft };
}
