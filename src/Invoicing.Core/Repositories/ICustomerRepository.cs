using Invoicing.Core.Parties;

namespace Invoicing.Core.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task SaveAsync(Customer customer);
}
