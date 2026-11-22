using CommunityToolkit.Mvvm.ComponentModel;
using Invoicing.App.Texts;
using Invoicing.Core.Parties;

namespace Invoicing.App.ViewModels;

/// <summary>Maintains the selected address book entry's editable values.</summary>
public partial class CustomerEditViewModel : ObservableObject
{
    private readonly int _id;
    [ObservableProperty] private string? _email;

    public CustomerEditViewModel(Customer customer)
    {
        _id = customer.Id;
        Form.Load(customer);
        Email = customer.Email;
        Form.PropertyChanged += (_, _) => NotifyValidation();
    }

    public PartyForm Form { get; } = new();
    public bool IsValid => ReadForm().Validate().Count == 0;
    public IEnumerable<string> Messages => ReadForm().Validate().Select(ValidationTexts.Format);

    public Customer ReadForm()
    {
        var customer = new Customer { Id = _id, Email = Email?.Trim() };
        Form.ApplyTo(customer);
        return customer;
    }

    partial void OnEmailChanged(string? value) => NotifyValidation();

    private void NotifyValidation()
    {
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(Messages));
    }
}
