using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Payments;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Documents;

/// <summary>Common state, transitions and validation for a billing document.</summary>
public abstract class Document
{
    private readonly List<InvoiceLine> _lines = [];
    private DateOnly? _issueDate;
    private DateOnly? _dueDate;
    private Customer? _customer;
    private Supplier? _supplier;

    protected Document(DocumentHeader header, IEnumerable<InvoiceLine> lines)
    {
        Id = header.Id;
        Number = header.Number;
        Status = header.Status;
        _issueDate = header.IssueDate;
        _dueDate = header.DueDate;
        _customer = header.Status == DocumentStatus.Draft ? header.Customer : header.Customer?.CreateSnapshot();
        _supplier = header.Supplier?.CreateSnapshot();
        _lines.AddRange(lines);
        Lines = _lines.AsReadOnly();
    }

    public abstract DocumentKind Kind { get; }
    public int Id { get; private set; }
    public string? Number { get; private set; }
    public DocumentStatus Status { get; private set; }
    public DateOnly? IssueDate
    {
        get => _issueDate;
        set { EnsureEditable(); _issueDate = value; }
    }

    public DateOnly? DueDate
    {
        get => _dueDate;
        set { EnsureEditable(); _dueDate = value; }
    }

    public Customer? Customer
    {
        get => IsEditable ? _customer : _customer?.CreateSnapshot();
        set { EnsureEditable(); _customer = value; }
    }

    public Supplier? Supplier => _supplier?.CreateSnapshot();
    public IReadOnlyList<InvoiceLine> Lines { get; }
    public bool IsEditable => Status == DocumentStatus.Draft;
    public virtual bool CanMarkAsPaid => false;
    public virtual bool CanCancel => false;
    public virtual bool IsOverdue(DateOnly today) => Status == DocumentStatus.Issued && DueDate < today;

    public void AssignId(int id)
    {
        if (Id != 0 || id <= 0)
        {
            throw new InvalidOperationException("A document must receive a positive identifier exactly once.");
        }

        Id = id;
    }

    public void ReplaceLines(IEnumerable<InvoiceLine> lines)
    {
        EnsureEditable();
        var replacement = lines.Select(line => Kind == DocumentKind.NonVatInvoice ? line.WithoutVat() : line).ToList();
        _lines.Clear();
        _lines.AddRange(replacement);
    }

    public decimal CalculateTotalBase() => _lines.Sum(line => line.CalculateBase());
    public abstract decimal CalculateTotal();
    public abstract IReadOnlyList<ValidationMessage> Validate(Supplier? supplier, VatRates vatRates);

    /// <summary>Freezes the current party details and assigns the official number.</summary>
    public void Issue(string number, Supplier supplier)
    {
        EnsureEditable();
        _customer = _customer?.CreateSnapshot();
        _supplier = supplier.CreateSnapshot();
        if (CzechBankAccount.TryParse(supplier.BankAccount, out var account))
        {
            _supplier.Iban = account.ToIban();
        }

        Number = number;
        Status = DocumentStatus.Issued;
    }

    public void MarkAsPaid()
    {
        if (!CanMarkAsPaid)
        {
            throw new InvalidOperationException("This document cannot be marked as paid.");
        }

        Status = DocumentStatus.Paid;
    }

    public void Cancel()
    {
        if (!CanCancel)
        {
            throw new InvalidOperationException("This document cannot be cancelled.");
        }

        Status = DocumentStatus.Cancelled;
    }

    protected void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException("Only draft documents can be changed.");
        }
    }

    protected List<ValidationMessage> ValidateCommon(Supplier? supplier, VatRates? rates, bool creditNote)
    {
        var messages = new List<ValidationMessage>();
        DocumentValidation.ValidateSupplier(messages, supplier, rates is not null);
        if (_customer is null)
        {
            messages.Add(new(ValidationCode.Missing, ValidationTarget.Customer));
        }
        else
        {
            messages.AddRange(_customer.Validate());
        }

        DocumentValidation.ValidateDates(messages, IssueDate, DueDate);
        DocumentValidation.ValidateLines(messages, Lines, rates, creditNote);
        return messages;
    }
}
