using System.Globalization;
using Invoicing.Core.Companies;

namespace Invoicing.Ares;

/// <summary>Builds editable company details from the register response.</summary>
public static class AresResponseMapper
{
    public static CompanyInfo Map(AresSubjectDto subject)
    {
        var address = subject.Address;
        if (address?.City is null || address.PostalCode is null || address.HouseNumber is null)
        {
            return new(subject.Ico ?? "", subject.Name ?? "", address?.FullAddress ?? "", "", "", subject.Dic);
        }

        return new(subject.Ico ?? "", subject.Name ?? "", FormatStreet(address), address.City,
            address.PostalCode.Value.ToString("00000", CultureInfo.InvariantCulture), subject.Dic);
    }

    private static string FormatStreet(AresAddressDto address)
    {
        var street = address.Street ?? address.District ?? address.City;
        var house = address.HouseNumber?.ToString(CultureInfo.InvariantCulture);
        var orientation = address.StreetNumber?.ToString(CultureInfo.InvariantCulture);
        return orientation is null ? $"{street} {house}" : $"{street} {house}/{orientation}{address.StreetNumberSuffix}";
    }
}
