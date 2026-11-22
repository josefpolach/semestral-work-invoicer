using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Services;
using Invoicing.App.Texts;
using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Payments;
using Invoicing.Core.Repositories;

namespace Invoicing.App.ViewModels;

/// <summary>Edits the application's single supplier and displays e-mail availability.</summary>
public partial class SupplierViewModel : ObservableObject
{
    private const int DefaultDueDays = 14;
    private readonly ISupplierRepository _suppliers;
    private readonly CompanyLookup _lookup;
    private readonly IDialogService _dialogs;
    [ObservableProperty] private bool _isVatPayer;
    [ObservableProperty] private string _registerEntry = string.Empty;
    [ObservableProperty] private string _bankAccount = string.Empty;
    [ObservableProperty] private string _issuedBy = string.Empty;
    [ObservableProperty] private decimal? _dueDays = DefaultDueDays;

    internal SupplierViewModel(ISupplierRepository suppliers, CompanyLookup lookup, IDialogService dialogs, EmailSettings email)
    {
        _suppliers = suppliers;
        _lookup = lookup;
        _dialogs = dialogs;
        EmailStatus = EmailTexts.Status(email);
        Form.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Messages));
    }

    public event Action? Saved;
    public PartyForm Form { get; } = new();
    public string EmailStatus { get; }
    public string Iban => CzechBankAccount.TryParse(BankAccount, out var account) ? account.ToIban() : UiTexts.InvalidBankAccount;
    public IEnumerable<string> Messages => ReadForm().Validate().Select(ValidationTexts.Format);

    public async Task LoadAsync()
    {
        var supplier = await _suppliers.GetAsync();
        if (supplier is null)
        {
            return;
        }

        Form.Load(supplier);
        IsVatPayer = supplier.IsVatPayer;
        RegisterEntry = supplier.RegisterEntry;
        BankAccount = supplier.BankAccount;
        IssuedBy = supplier.IssuedBy;
        DueDays = supplier.DefaultDueDays;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.PropertyName is nameof(BankAccount) or nameof(IsVatPayer) or nameof(RegisterEntry) or nameof(IssuedBy))
        {
            OnPropertyChanged(nameof(Messages));
        }

        if (args.PropertyName == nameof(BankAccount))
        {
            OnPropertyChanged(nameof(Iban));
        }
    }

    [RelayCommand]
    private Task LoadCompanyAsync() => _lookup.ApplyAsync(Form);

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _suppliers.SaveAsync(ReadForm());
        Saved?.Invoke();
        await _dialogs.ShowMessageAsync(UiTexts.SupplierSaved);
    }

    private Supplier ReadForm()
    {
        var supplier = new Supplier
        {
            IsVatPayer = IsVatPayer,
            RegisterEntry = RegisterEntry?.Trim() ?? string.Empty,
            BankAccount = BankAccount?.Trim() ?? string.Empty,
            IssuedBy = IssuedBy?.Trim() ?? string.Empty,
            DefaultDueDays = (int)(DueDays ?? 0),
            Iban = CzechBankAccount.TryParse(BankAccount, out var account) ? account.ToIban() : string.Empty
        };
        Form.ApplyTo(supplier);
        return supplier;
    }
}
