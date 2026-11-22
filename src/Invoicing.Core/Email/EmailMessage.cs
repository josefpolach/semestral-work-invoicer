namespace Invoicing.Core.Email;

public record EmailMessage(string FromAddress, string FromName, string ToAddress,
    string Subject, string HtmlBody, string TextBody, EmailAttachment Attachment);
