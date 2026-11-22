using Invoicing.Core.Payments;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Parties;

/// <summary>The business issuing documents.</summary>
public class Supplier : Party
{
    public bool IsVatPayer { get; set; }
    public string RegisterEntry { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public int DefaultDueDays { get; set; }

    public IReadOnlyList<ValidationMessage> Validate()
    {
        var messages = new List<ValidationMessage>();
        ValidateNameAndAddress(messages, ValidationTarget.Supplier);
        ValidateDicFormatIfPresent(messages, ValidationTarget.Supplier);
        ValidateRegistration(messages);
        ValidateIssueDetails(messages);
        return messages;
    }

    public Supplier CreateSnapshot() => (Supplier)MemberwiseClone();

    private void ValidateRegistration(List<ValidationMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(Ico))
        {
            messages.Add(new(ValidationCode.IcoMissing, ValidationTarget.Supplier));
        }
        else if (!IcoValidator.IsValid(Ico))
        {
            messages.Add(new(ValidationCode.IcoInvalid, ValidationTarget.Supplier));
        }

        if (IsVatPayer && string.IsNullOrWhiteSpace(Dic))
        {
            messages.Add(new(ValidationCode.DicMissing, ValidationTarget.Supplier));
        }
    }

    private void ValidateIssueDetails(List<ValidationMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(RegisterEntry))
        {
            messages.Add(new(ValidationCode.RegisterEntryMissing, ValidationTarget.Supplier));
        }

        if (!CzechBankAccount.TryParse(BankAccount, out _))
        {
            messages.Add(new(ValidationCode.BankAccountInvalid, ValidationTarget.Supplier));
        }

        if (string.IsNullOrWhiteSpace(IssuedBy))
        {
            messages.Add(new(ValidationCode.IssuedByMissing, ValidationTarget.Supplier));
        }
    }
}
