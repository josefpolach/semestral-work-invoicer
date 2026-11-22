using System.Net.Mail;

namespace Invoicing.Core.Validation;

/// <summary>Accepts a single plain e-mail address.</summary>
public static class EmailAddressValidator
{
    public static bool IsValid(string? value) =>
        value is not null && MailAddress.TryCreate(value, out var address) && address.Address == value.Trim();
}
