namespace Invoicing.Core.Validation;

public enum ValidationCode
{
    Missing, NameMissing, AddressIncomplete, IcoMissing, IcoInvalid, DicMissing, DicInvalid,
    RegisterEntryMissing, BankAccountInvalid, IssuedByMissing, EmailInvalid, MustBeVatPayer,
    MustNotBeVatPayer, NoLines, DescriptionMissing, QuantityNotPositive, QuantityZero,
    UnitPriceNegative, VatRateNotAllowed, DiscountOutOfRange, IssueDateMissing, DueDateMissing,
    TaxableSupplyDateMissing, DueDateBeforeIssueDate, IssuedLongAfterTaxableSupply,
    CorrectionReasonMissing, OriginalInvoiceMissing
}
