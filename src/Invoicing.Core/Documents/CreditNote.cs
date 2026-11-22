using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Documents;

/// <summary>A corrective tax document linked to the original invoice.</summary>
public class CreditNote(DocumentHeader header, IEnumerable<InvoiceLine> lines, DateOnly? taxableSupplyDate,
    DocumentReference? originalInvoice) : Document(header, lines)
{
    private DateOnly? _taxableSupplyDate = taxableSupplyDate;
    private DocumentReference? _originalInvoice = originalInvoice;
    private string _correctionReason = string.Empty;

    public static CreditNote Restore(DocumentHeader header, IEnumerable<InvoiceLine> lines,
        DateOnly? supplyDate, CreditNoteDetails details) =>
        new(header, lines, supplyDate, details.OriginalInvoice) { _correctionReason = details.CorrectionReason };

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
        var messages = ValidateCommon(supplier, vatRates, true);
        DocumentValidation.ValidateSupplyDate(messages, TaxableSupplyDate, null);
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
