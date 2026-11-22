using Invoicing.Core.Validation;

namespace Invoicing.Core.Parties;

/// <summary>Common details of a supplier or customer.</summary>
public abstract class Party
{
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Ico { get; set; }
    public string? Dic { get; set; }

    public bool HasCompleteAddress => !string.IsNullOrWhiteSpace(Street) &&
        !string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(PostalCode);

    protected void ValidateNameAndAddress(List<ValidationMessage> messages, ValidationTarget target)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            messages.Add(new(ValidationCode.NameMissing, target));
        }

        if (!HasCompleteAddress)
        {
            messages.Add(new(ValidationCode.AddressIncomplete, target));
        }
    }

    protected void ValidateDicFormatIfPresent(List<ValidationMessage> messages, ValidationTarget target)
    {
        if (!string.IsNullOrWhiteSpace(Dic) && !DicValidator.IsValid(Dic))
        {
            messages.Add(new(ValidationCode.DicInvalid, target));
        }
    }
}
