using Invoicing.Core.Parties;

namespace Invoicing.Core.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetAsync();
    Task SaveAsync(Supplier supplier);
}
