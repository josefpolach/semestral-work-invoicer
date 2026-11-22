using CommunityToolkit.Mvvm.ComponentModel;
using Invoicing.Core.Companies;
using Invoicing.Core.Parties;

namespace Invoicing.App.ViewModels;

/// <summary>Editable address fields shared by the supplier and customer forms.</summary>
public partial class PartyForm : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _street = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _postalCode = string.Empty;
    [ObservableProperty] private string? _ico;
    [ObservableProperty] private string? _dic;

    public void Load(Party party)
    {
        Name = party.Name;
        Street = party.Street;
        City = party.City;
        PostalCode = party.PostalCode;
        Ico = party.Ico;
        Dic = party.Dic;
    }

    public void Load(CompanyInfo company)
    {
        Name = company.Name;
        Street = company.Street;
        City = company.City;
        PostalCode = company.PostalCode;
        Ico = company.Ico;
        Dic = company.Dic;
    }

    public void ApplyTo(Party party)
    {
        party.Name = Name?.Trim() ?? string.Empty;
        party.Street = Street?.Trim() ?? string.Empty;
        party.City = City?.Trim() ?? string.Empty;
        party.PostalCode = PostalCode?.Trim() ?? string.Empty;
        party.Ico = Ico?.Trim();
        party.Dic = Dic?.Trim();
    }
}
