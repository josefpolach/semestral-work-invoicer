using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Documents;

/// <summary>A corrective tax document linked to the original invoice.</summary>
public class CreditNote : Document
{
    private DateOnly? _taxableSupplyDate;
    private DocumentReference? _originalInvoice;
    private string _correctionReason;

    public CreditNote(DocumentHeader header, IEnumerable<InvoiceLine> lines, DateOnly? taxableSupplyDate,
        DocumentReference? originalInvoice) : this(header, lines, taxableSupplyDate, new CreditNoteDetails(originalInvoice, string.Empty))
    {
    }

    private CreditNote(DocumentHeader header, IEnumerable<InvoiceLine> lines, DateOnly? taxableSupplyDate,
        CreditNoteDetails details) : base(header, lines)
    {
        _taxableSupplyDate = taxableSupplyDate;
        _originalInvoice = details.OriginalInvoice;
        _correctionReason = details.CorrectionReason;
    }

    public static CreditNote Restore(DocumentHeader header, IEnumerable<InvoiceLine> lines,
        DateOnly? supplyDate, CreditNoteDetails details) => new(header, lines, supplyDate, details);

    public override DocumentKind Kind => DocumentKind.CreditNote;
    public DateOnly? TaxableSupplyDate
    {
        get => _taxableSupplyDate;
        set { EnsureEditable(); _taxableSupplyDate = value; }
    }

    public DocumentReference? OriginalInvoice
    {
        get => _originalInvoice;
        set { EnsureEditable(); _originalInvoice = value; }
    }

    public string CorrectionReason
    {
        get => _correctionReason;
        set { EnsureEditable(); _correctionReason = value; }
    }

    public VatSummary CalculateVatSummary() => VatSummary.Calculate(Lines);
    public override decimal CalculateTotal() => CalculateVatSummary().Total;
    public override bool IsOverdue(DateOnly today) => false;

    public override IReadOnlyList<ValidationMessage> Validate(Supplier? supplier, VatRates vatRates)
    {
        var messages = ValidateCommon(supplier, vatRates, needsVatPayer: true, allowNegativeQuantity: true);
        DocumentValidation.ValidateSupplyDateSet(messages, TaxableSupplyDate);
        if (OriginalInvoice is null)
        {
            messages.Add(new(ValidationCode.OriginalInvoiceMissing, ValidationTarget.Document));
        }

        if (string.IsNullOrWhiteSpace(CorrectionReason))
        {
            messages.Add(new(ValidationCode.CorrectionReasonMissing, ValidationTarget.Document));
        }

        return messages;
    }
}
