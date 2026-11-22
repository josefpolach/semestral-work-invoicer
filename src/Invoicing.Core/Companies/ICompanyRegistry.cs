namespace Invoicing.Core.Companies;

public interface ICompanyRegistry
{
    Task<CompanyLookupResult> FindByIcoAsync(string ico, CancellationToken cancellationToken);
}
