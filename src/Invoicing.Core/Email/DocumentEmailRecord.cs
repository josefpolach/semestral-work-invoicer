namespace Invoicing.Core.Email;

public record DocumentEmailRecord(string SentTo, DateTimeOffset SentAt, string ProviderRequestId);
