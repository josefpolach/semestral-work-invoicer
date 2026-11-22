using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Services;
using Invoicing.Core.Parties;
using Invoicing.Core.Repositories;

namespace Invoicing.App.ViewModels;

/// <summary>Searches, creates and edits address book entries.</summary>
public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomerRepository _customers;
    private readonly CompanyLookup _lookup;
    private IReadOnlyList<Customer> _all = [];
    [ObservableProperty] private IReadOnlyList<Customer> _items = [];
    [ObservableProperty] private Customer? _selected;
    [ObservableProperty] private string? _search;
    [ObservableProperty] private CustomerEditViewModel _editor = new(new());

    internal CustomersViewModel(ICustomerRepository customers, CompanyLookup lookup)
    {
        _customers = customers;
        _lookup = lookup;
        Editor.PropertyChanged += HandleEditorChanged;
    }

    public async Task LoadAsync()
    {
        _all = await _customers.GetAllAsync();
        Filter();
    }

    partial void OnSearchChanged(string? value) => Filter();

    partial void OnSelectedChanged(Customer? value)
    {
        if (value is not null)
        {
            Editor = new(value);
        }
    }

    partial void OnEditorChanged(CustomerEditViewModel? oldValue, CustomerEditViewModel newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= HandleEditorChanged;
        }

        newValue.PropertyChanged += HandleEditorChanged;
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void New()
    {
        Selected = null;
        Editor = new(new());
    }

    [RelayCommand]
    private Task LoadCompanyAsync() => _lookup.ApplyAsync(Editor.Form);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var customer = Editor.ReadForm();
        await _customers.SaveAsync(customer);
        await LoadAsync();
        Selected = Items.FirstOrDefault(item => item.Id == customer.Id);
        Editor = new(customer);
    }

    private bool CanSave() => Editor.IsValid;

    private void HandleEditorChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args) =>
        SaveCommand.NotifyCanExecuteChanged();

    private void Filter()
    {
        var query = Search?.Trim() ?? string.Empty;
        Items = _all.Where(customer => Includes(customer.Name, query) ||
            Includes(customer.Ico, query) || Includes(customer.City, query)).ToList();
    }

    private static bool Includes(string? value, string query) =>
        value?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true;
}
