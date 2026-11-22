namespace Invoicing.Core.Documents;

/// <summary>Persisted reference and reason for a correction.</summary>
public record CreditNoteDetails(DocumentReference? OriginalInvoice, string CorrectionReason);
