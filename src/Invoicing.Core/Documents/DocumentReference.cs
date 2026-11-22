namespace Invoicing.Core.Documents;

/// <summary>Identifies the invoice corrected by a credit note.</summary>
public record DocumentReference(int Id, string Number);
