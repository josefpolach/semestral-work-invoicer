using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Documents;

/// <summary>An invoice issued by a non-payer of VAT.</summary>
public class NonVatInvoice(DocumentHeader header, IEnumerable<InvoiceLine> lines)
    : Document(header, lines.Select(line => line.WithoutVat()))
{
    public override DocumentKind Kind => DocumentKind.NonVatInvoice;
    public override bool CanMarkAsPaid => Status == DocumentStatus.Issued;
    public override bool CanCancel => Status == DocumentStatus.Issued;
    public override decimal CalculateTotal() => CalculateTotalBase();

    protected override InvoiceLine PrepareLine(InvoiceLine line) => line.WithoutVat();

    public override IReadOnlyList<ValidationMessage> Validate(Supplier? supplier, VatRates vatRates) =>
        ValidateCommon(supplier, rates: null, needsVatPayer: false, allowNegativeQuantity: false);
}
