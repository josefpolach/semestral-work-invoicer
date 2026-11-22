using Dapper;
using Invoicing.Core.Parties;
using Invoicing.Core.Repositories;

namespace Invoicing.Data;

/// <summary>Persists the customer address book.</summary>
public class CustomerRepository(SqliteConnectionFactory factory) : ICustomerRepository
{
    private const string Read = "SELECT * FROM customers ORDER BY name, id";
    private const string Insert = """
        INSERT INTO customers (name, street, city, postal_code, ico, dic, email)
        VALUES (@Name, @Street, @City, @PostalCode, @Ico, @Dic, @Email)
        RETURNING id
        """;
    private const string Update = """
        UPDATE customers SET name = @Name,
            street = @Street,
            city = @City,
            postal_code = @PostalCode,
            ico = @Ico,
            dic = @Dic,
            email = @Email
        WHERE id = @Id
        """;

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        await using var connection = await factory.OpenConnectionAsync();
        return (await connection.QueryAsync<Customer>(Read)).ToList();
    }

    public async Task SaveAsync(Customer customer)
    {
        await using var connection = await factory.OpenConnectionAsync();
        if (customer.Id == 0)
        {
            customer.Id = await connection.QuerySingleAsync<int>(Insert, customer);
        }
        else if (await connection.ExecuteAsync(Update, customer) != 1)
        {
            throw new InvalidOperationException("The customer no longer exists.");
        }
    }
}
