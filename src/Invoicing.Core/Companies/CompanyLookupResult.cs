namespace Invoicing.Core.Companies;

/// <summary>Company details or the categorized reason a lookup failed.</summary>
public class CompanyLookupResult
{
    private CompanyLookupResult(CompanyInfo? company, CompanyLookupError? error)
    {
        Company = company;
        Error = error;
    }

    public CompanyInfo? Company { get; }
    public CompanyLookupError? Error { get; }
    public bool IsSuccess => Company is not null;

    public static CompanyLookupResult Success(CompanyInfo company) => new(company, null);
    public static CompanyLookupResult Failure(CompanyLookupError error) => new(null, error);
}
