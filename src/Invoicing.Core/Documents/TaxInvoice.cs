using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Documents;

/// <summary>An invoice issued by a VAT payer.</summary>
public class TaxInvoice(DocumentHeader header, IEnumerable<InvoiceLine> lines, DateOnly? taxableSupplyDate)
    : Document(header, lines)
{
    private DateOnly? _taxableSupplyDate = taxableSupplyDate;

    public override DocumentKind Kind => DocumentKind.TaxInvoice;
    public override bool CanMarkAsPaid => Status == DocumentStatus.Issued;
    public bool CanCreateCreditNote => Status is DocumentStatus.Issued or DocumentStatus.Paid;
    public DateOnly? TaxableSupplyDate
    {
        get => _taxableSupplyDate;
        set { EnsureEditable(); _taxableSupplyDate = value; }
    }

    public VatSummary CalculateVatSummary() => VatSummary.Calculate(Lines);
    public override decimal CalculateTotal() => CalculateVatSummary().Total;

    public override IReadOnlyList<ValidationMessage> Validate(Supplier? supplier, VatRates vatRates)
    {
        var messages = ValidateCommon(supplier, vatRates, needsVatPayer: true, allowNegativeQuantity: false);
        DocumentValidation.ValidateSupplyDateSet(messages, TaxableSupplyDate);
        DocumentValidation.WarnIfIssuedLate(messages, TaxableSupplyDate, IssueDate);
        return messages;
    }

    public CreditNote CreateCreditNote(DateOnly today, int dueDays)
    {
        if (!CanCreateCreditNote || Number is null)
        {
            throw new InvalidOperationException("Only numbered tax invoices can be corrected.");
        }

        return new(DocumentHeader.NewDraft(Customer, today, today.AddDays(dueDays)),
            Lines.Select(line => line.WithNegatedQuantity()), today, new(Id, Number));
    }
}
