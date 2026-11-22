using Invoicing.Core.Companies;

namespace Invoicing.App.Texts;

internal static class CompanyLookupTexts
{
    public static string Format(CompanyLookupError? error) => error switch
    {
        CompanyLookupError.InvalidIco => "Neplatné IČO. IČO musí mít 8 číslic a správnou kontrolní číslici.",
        CompanyLookupError.NotFound => "Subjekt s tímto IČO nebyl v ARES nalezen.",
        CompanyLookupError.Timeout => "ARES neodpověděl včas. Zkuste to znovu.",
        CompanyLookupError.NoConnection => "Nepodařilo se spojit s ARES. Zkontrolujte připojení k internetu.",
        CompanyLookupError.UnexpectedResponse => "ARES vrátil neočekávanou odpověď.",
        _ => throw new ArgumentOutOfRangeException(nameof(error))
    };
}
