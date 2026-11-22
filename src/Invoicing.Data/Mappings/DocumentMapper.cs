using System.Globalization;
using Invoicing.Core.Documents;
using Invoicing.Core.Parties;
using Invoicing.Data.Rows;

namespace Invoicing.Data.Mappings;

internal static class DocumentMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static Document Read(DocumentRow row, IEnumerable<DocumentLineRow> lines)
    {
        var kind = Enum.Parse<DocumentKind>(row.DocumentType);
        var header = ReadHeader(row, kind);
        var mapped = lines.Select(DocumentLineMapper.Read);
        var supplyDate = Parse(row.TaxableSupplyDate);
        var reference = row.OriginalDocumentId is int id && row.OriginalNumber is string number
            ? new DocumentReference(id, number) : null;
        return kind switch
        {
            DocumentKind.TaxInvoice => new TaxInvoice(header, mapped, supplyDate),
            DocumentKind.NonVatInvoice => new NonVatInvoice(header, mapped),
            DocumentKind.CreditNote => CreditNote.Restore(header, mapped, supplyDate,
                new(reference, row.CorrectionReason ?? string.Empty)),
            _ => throw new InvalidOperationException("Unknown stored document type.")
        };
    }

    public static DocumentRow Write(Document document)
    {
        var customer = document.Customer ?? throw new InvalidOperationException("A customer must be selected.");
        var row = new DocumentRow
        {
            Id = document.Id,
            CustomerId = customer.Id,
            DocumentType = document.Kind.ToString(),
            Status = document.Status.ToString(),
            Number = document.Number,
            IssueDate = Format(document.IssueDate),
            DueDate = Format(document.DueDate)
        };
        WriteSupplyAndCorrection(document, row);
        if (!document.IsEditable)
        {
            WriteParties(row, customer, document.Supplier);
        }

        return row;
    }

    private static DocumentHeader ReadHeader(DocumentRow row, DocumentKind kind) => new()
    {
        Id = row.Id,
        Number = row.Number,
        Status = Enum.Parse<DocumentStatus>(row.Status),
        IssueDate = Parse(row.IssueDate),
        DueDate = Parse(row.DueDate),
        Customer = ReadCustomer(row),
        Supplier = ReadSupplier(row, kind)
    };

    private static Customer ReadCustomer(DocumentRow row) => new()
    {
        Id = row.CustomerId,
        Name = row.CustomerName ?? string.Empty,
        Street = row.CustomerStreet ?? string.Empty,
        City = row.CustomerCity ?? string.Empty,
        PostalCode = row.CustomerPostalCode ?? string.Empty,
        Ico = row.CustomerIco,
        Dic = row.CustomerDic,
        Email = row.CustomerEmail,
    };

    private static Supplier? ReadSupplier(DocumentRow row, DocumentKind kind) => row.SupplierName is null ? null : new()
    {
        IsVatPayer = kind != DocumentKind.NonVatInvoice,
        Name = row.SupplierName,
        Street = row.SupplierStreet ?? string.Empty,
        City = row.SupplierCity ?? string.Empty,
        PostalCode = row.SupplierPostalCode ?? string.Empty,
        Ico = row.SupplierIco,
        Dic = row.SupplierDic,
        RegisterEntry = row.SupplierRegisterEntry ?? string.Empty,
        BankAccount = row.SupplierBankAccount ?? string.Empty,
        Iban = row.SupplierIban ?? string.Empty,
        IssuedBy = row.SupplierIssuedBy ?? string.Empty,
    };

    private static void WriteSupplyAndCorrection(Document document, DocumentRow row)
    {
        if (document is TaxInvoice invoice)
        {
            row.TaxableSupplyDate = Format(invoice.TaxableSupplyDate);
        }
        else if (document is CreditNote credit)
        {
            row.TaxableSupplyDate = Format(credit.TaxableSupplyDate);
            row.OriginalDocumentId = credit.OriginalInvoice?.Id;
            row.CorrectionReason = credit.CorrectionReason;
        }
    }

    private static void WriteParties(DocumentRow row, Customer customer, Supplier? supplier)
    {
        row.CustomerName = customer.Name;
        row.CustomerStreet = customer.Street;
        row.CustomerCity = customer.City;
        row.CustomerPostalCode = customer.PostalCode;
        row.CustomerIco = customer.Ico;
        row.CustomerDic = customer.Dic;
        row.CustomerEmail = customer.Email;
        row.SupplierName = supplier?.Name;
        row.SupplierStreet = supplier?.Street;
        row.SupplierCity = supplier?.City;
        row.SupplierPostalCode = supplier?.PostalCode;
        row.SupplierIco = supplier?.Ico;
        row.SupplierDic = supplier?.Dic;
        row.SupplierRegisterEntry = supplier?.RegisterEntry;
        row.SupplierBankAccount = supplier?.BankAccount;
        row.SupplierIban = supplier?.Iban;
        row.SupplierIssuedBy = supplier?.IssuedBy;
    }

    private static DateOnly? Parse(string? value) =>
        value is null ? null : DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);

    private static string? Format(DateOnly? value) => value?.ToString(DateFormat, CultureInfo.InvariantCulture);
}
