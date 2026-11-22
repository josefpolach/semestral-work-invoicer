using Invoicing.Core.Configuration;
using Invoicing.Core.Parties;
using Invoicing.Core.Validation;

namespace Invoicing.Core.Documents;

/// <summary>Validation shared by the three document kinds.</summary>
internal static class DocumentValidation
{
    private const int MaxDaysBetweenSupplyAndIssue = 15;

    public static void ValidateSupplier(List<ValidationMessage> messages, Supplier? supplier, bool needsVatPayer)
    {
        if (supplier is null)
        {
            messages.Add(new(ValidationCode.Missing, ValidationTarget.Supplier));
            return;
        }

        messages.AddRange(supplier.Validate());
        if (supplier.IsVatPayer != needsVatPayer)
        {
            messages.Add(new(needsVatPayer ? ValidationCode.MustBeVatPayer : ValidationCode.MustNotBeVatPayer,
                ValidationTarget.Supplier));
        }
    }

    public static void ValidateDates(List<ValidationMessage> messages, DateOnly? issueDate, DateOnly? dueDate)
    {
        if (issueDate is null)
        {
            messages.Add(new(ValidationCode.IssueDateMissing, ValidationTarget.Document));
        }

        if (dueDate is null)
        {
            messages.Add(new(ValidationCode.DueDateMissing, ValidationTarget.Document));
        }
        else if (dueDate < issueDate)
        {
            messages.Add(new(ValidationCode.DueDateBeforeIssueDate, ValidationTarget.Document));
        }
    }

    public static void ValidateSupplyDate(List<ValidationMessage> messages, DateOnly? supplyDate, DateOnly? issueDate)
    {
        if (supplyDate is null)
        {
            messages.Add(new(ValidationCode.TaxableSupplyDateMissing, ValidationTarget.Document));
        }
        else if (issueDate is DateOnly issue && issue.DayNumber - supplyDate.Value.DayNumber > MaxDaysBetweenSupplyAndIssue)
        {
            // Section 28 of the VAT Act sets the issuance deadline for tax invoices.
            messages.Add(new(ValidationCode.IssuedLongAfterTaxableSupply, ValidationTarget.Document,
                ValidationSeverity.Warning));
        }
    }

    public static void ValidateLines(List<ValidationMessage> messages, IReadOnlyList<InvoiceLine> lines,
        VatRates? rates, bool creditNote)
    {
        if (lines.Count == 0)
        {
            messages.Add(new(ValidationCode.NoLines, ValidationTarget.Document));
        }

        for (var index = 0; index < lines.Count; index++)
        {
            foreach (var code in GetLineIssues(lines[index], rates, creditNote))
            {
                messages.Add(new(code, ValidationTarget.Line, LinePosition: index + 1));
            }
        }
    }

    private static IEnumerable<ValidationCode> GetLineIssues(InvoiceLine line, VatRates? rates, bool creditNote)
    {
        if (string.IsNullOrWhiteSpace(line.Description))
        {
            yield return ValidationCode.DescriptionMissing;
        }

        if (creditNote ? line.Quantity == 0 : line.Quantity <= 0)
        {
            yield return creditNote ? ValidationCode.QuantityZero : ValidationCode.QuantityNotPositive;
        }

        if (line.UnitPriceWithoutVat < 0)
        {
            yield return ValidationCode.UnitPriceNegative;
        }

        if (rates is not null && !rates.IsAllowed(line.VatRate))
        {
            yield return ValidationCode.VatRateNotAllowed;
        }

        if (line.DiscountPercent is < 0 or > 100)
        {
            yield return ValidationCode.DiscountOutOfRange;
        }
    }
}
