using Invoicing.Core.Parties;

namespace Invoicing.Data;

/// <summary>Maps the SQLite integer VAT flag to the supplier's Boolean value.</summary>
internal class SupplierRow
{
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Ico { get; set; }
    public string? Dic { get; set; }
    public int IsVatPayer { get; set; }
    public string RegisterEntry { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public int DefaultDueDays { get; set; }

    public Supplier ToSupplier() => new()
    {
        Name = Name,
        Street = Street,
        City = City,
        PostalCode = PostalCode,
        Ico = Ico,
        Dic = Dic,
        IsVatPayer = IsVatPayer != 0,
        RegisterEntry = RegisterEntry,
        BankAccount = BankAccount,
        Iban = Iban,
        IssuedBy = IssuedBy,
        DefaultDueDays = DefaultDueDays
    };
}
