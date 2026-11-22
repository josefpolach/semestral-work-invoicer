using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Texts;
using Invoicing.Core.Configuration;
using Invoicing.Core.Documents;

namespace Invoicing.App.ViewModels;

/// <summary>Tracks editable invoice rows and their numeric input errors.</summary>
public partial class DocumentLinesViewModel : ObservableObject
{
    private readonly IReadOnlyList<decimal> _vatRates;
    private readonly decimal _defaultRate;
    private bool _canEdit;
    [ObservableProperty] private InvoiceLineViewModel? _selectedLine;

    internal DocumentLinesViewModel(VatRates rates, bool hasVat)
    {
        _vatRates = hasVat ? rates.All : [];
        _defaultRate = hasVat ? rates.Standard : 0;
    }

    internal event Action? Changed;
    public ObservableCollection<InvoiceLineViewModel> Items { get; } = [];
    internal bool AreValid => Items.All(line => line.IsValid);

    internal void Load(IReadOnlyList<InvoiceLine> lines)
    {
        foreach (var line in Items)
        {
            line.PropertyChanged -= HandleLineChanged;
        }

        SelectedLine = null;
        Items.Clear();
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    internal void SetEditable(bool canEdit)
    {
        _canEdit = canEdit;
        AddLineCommand.NotifyCanExecuteChanged();
        RemoveLineCommand.NotifyCanExecuteChanged();
    }

    internal IEnumerable<InvoiceLine> ReadLines() => Items.Select(line => line.ToLine());

    internal IEnumerable<string> InputErrors()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (!Items[index].IsValid)
            {
                yield return ValidationTexts.Line(index + 1, DocumentTexts.InvalidNumber);
            }
        }
    }

    partial void OnSelectedLineChanged(InvoiceLineViewModel? value) => RemoveLineCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void AddLine()
    {
        SelectedLine = AppendLine(new(string.Empty, 1, UiTexts.DefaultLineUnit, 0, _defaultRate, 0));
        Changed?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void RemoveLine()
    {
        if (SelectedLine is InvoiceLineViewModel line)
        {
            line.PropertyChanged -= HandleLineChanged;
            Items.Remove(line);
            SelectedLine = null;
            Changed?.Invoke();
        }
    }

    private bool CanEdit() => _canEdit;
    private bool CanRemove() => _canEdit && SelectedLine is not null;

    private InvoiceLineViewModel AppendLine(InvoiceLine line)
    {
        var item = new InvoiceLineViewModel(line, _vatRates);
        item.PropertyChanged += HandleLineChanged;
        Items.Add(item);
        return item;
    }

    private void HandleLineChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(InvoiceLineViewModel.Description) or nameof(InvoiceLineViewModel.Quantity)
            or nameof(InvoiceLineViewModel.Unit) or nameof(InvoiceLineViewModel.UnitPrice)
            or nameof(InvoiceLineViewModel.Discount) or nameof(InvoiceLineViewModel.VatRate))
        {
            Changed?.Invoke();
        }
    }
}
