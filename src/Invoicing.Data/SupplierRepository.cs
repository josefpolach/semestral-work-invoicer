using Dapper;
using Invoicing.Core.Parties;
using Invoicing.Core.Repositories;

namespace Invoicing.Data;

/// <summary>Persists the application supplier.</summary>
public class SupplierRepository(SqliteConnectionFactory factory) : ISupplierRepository
{
    private const string Read = "SELECT * FROM supplier WHERE id = 1";
    private const string Insert = """
        INSERT INTO supplier (id, name, street, city, postal_code, ico, dic, is_vat_payer, register_entry, bank_account, iban, issued_by, default_due_days)
        VALUES (1, @Name, @Street, @City, @PostalCode, @Ico, @Dic, @IsVatPayer, @RegisterEntry, @BankAccount, @Iban, @IssuedBy, @DefaultDueDays)
        ON CONFLICT(id) DO UPDATE SET
            name = excluded.name,
            street = excluded.street,
            city = excluded.city,
            postal_code = excluded.postal_code,
            ico = excluded.ico,
            dic = excluded.dic,
            is_vat_payer = excluded.is_vat_payer,
            register_entry = excluded.register_entry,
            bank_account = excluded.bank_account,
            iban = excluded.iban,
            issued_by = excluded.issued_by,
            default_due_days = excluded.default_due_days
        """;

    public async Task<Supplier?> GetAsync()
    {
        await using var connection = await factory.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<SupplierRow>(Read);
        return row?.ToSupplier();
    }

    public async Task SaveAsync(Supplier supplier)
    {
        await using var connection = await factory.OpenConnectionAsync();
        await connection.ExecuteAsync(Insert, new
        {
            supplier.Name,
            supplier.Street,
            supplier.City,
            supplier.PostalCode,
            Ico = supplier.Ico ?? string.Empty,
            supplier.Dic,
            IsVatPayer = supplier.IsVatPayer ? 1 : 0,
            supplier.RegisterEntry,
            supplier.BankAccount,
            supplier.Iban,
            supplier.IssuedBy,
            supplier.DefaultDueDays
        });
    }
}
