namespace Invoicing.Core.Repositories;

/// <summary>Reports that a document was removed or changed before a guarded database write.</summary>
public class DocumentStateChangedException() : InvalidOperationException("The document no longer has the expected status.");
