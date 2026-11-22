using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Invoicing.App.Texts;
using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Services;
using Invoicing.Core.Validation;
using Microsoft.Data.Sqlite;

namespace Invoicing.App.ViewModels;

/// <summary>Previews an e-mail and reuses the same request key when sending is retried.</summary>
public partial class SendEmailViewModel : ObservableObject
{
    private readonly Document _document;
    private readonly EmailMessage _message;
    private readonly DocumentEmailService _service;
    private readonly string _idempotencyKey = Guid.NewGuid().ToString();
    [ObservableProperty] private string? _recipient;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _isSending;
    [ObservableProperty] private bool _recipientLocked;

    public SendEmailViewModel(Document document, EmailMessage message, DocumentEmailService service,
        DocumentEmailRecord? latest)
    {
        _document = document;
        _message = message;
        _service = service;
        _recipient = message.ToAddress;
        PreviouslySent = latest is null ? null : EmailTexts.PreviouslySent(latest);
    }

    public event Action<bool>? CloseRequested;
    public string Subject => _message.Subject;
    public string Body => _message.TextBody;
    public string Attachment => _message.Attachment.FileName;
    public string? PreviouslySent { get; }
    public bool IsRecipientReadOnly => IsSending || RecipientLocked;
    public string? RecipientError => EmailAddressValidator.IsValid(Recipient) ? null : EmailTexts.InvalidRecipient;

    partial void OnRecipientChanged(string? value)
    {
        OnPropertyChanged(nameof(RecipientError));
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSendingChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsRecipientReadOnly));
    }

    partial void OnRecipientLockedChanged(bool value) => OnPropertyChanged(nameof(IsRecipientReadOnly));

    private bool CanSend() => !IsSending && RecipientError is null;
    private bool CanCancel() => !IsSending;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (Recipient is not string recipient || !CanSend())
        {
            return;
        }

        if (await SendWithProgressAsync(recipient.Trim()))
        {
            CloseRequested?.Invoke(true);
        }
    }

    private async Task<bool> SendWithProgressAsync(string recipient)
    {
        IsSending = true;
        Error = null;
        try
        {
            return await TrySendAsync(recipient);
        }
        finally
        {
            IsSending = false;
        }
    }

    private async Task<bool> TrySendAsync(string recipient)
    {
        try
        {
            var result = await _service.SendAsync(_document, _message with { ToAddress = recipient },
                _idempotencyKey, DateTimeOffset.Now, CancellationToken.None);
            // A retry after uncertain delivery must use the same payload and request key.
            RecipientLocked |= result.Error is EmailSendError.NoConnection or EmailSendError.ServiceUnavailable
                or EmailSendError.PreviousAttemptInProgress or EmailSendError.UnexpectedResponse;
            Error = result.IsSuccess ? null : EmailTexts.Format(result);
            return result.IsSuccess;
        }
        catch (SqliteException)
        {
            RecipientLocked = true;
            Error = EmailTexts.ReceiptSaveFailed;
            return false;
        }
    }
}
