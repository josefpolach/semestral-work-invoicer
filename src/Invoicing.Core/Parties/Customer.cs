using Invoicing.Core.Validation;

namespace Invoicing.Core.Parties;

/// <summary>An address book entry or an issued document's customer snapshot.</summary>
public class Customer : Party
{
    public int Id { get; set; }
    public string? Email { get; set; }

    public IReadOnlyList<ValidationMessage> Validate()
    {
        const ValidationTarget target = ValidationTarget.Customer;
        var messages = new List<ValidationMessage>();
        ValidateNameAndAddress(messages, target);
        ValidateDicFormatIfPresent(messages, target);

        if (!string.IsNullOrWhiteSpace(Ico) && !IcoValidator.IsValid(Ico))
        {
            messages.Add(new(ValidationCode.IcoInvalid, target));
        }

        if (!string.IsNullOrWhiteSpace(Email) && !EmailAddressValidator.IsValid(Email))
        {
            messages.Add(new(ValidationCode.EmailInvalid, target));
        }

        return messages;
    }

    public Customer CreateSnapshot() => (Customer)MemberwiseClone();
}
