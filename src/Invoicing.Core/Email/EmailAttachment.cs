namespace Invoicing.Core.Email;

public record EmailAttachment(string FileName, string ContentType, byte[] Content);
