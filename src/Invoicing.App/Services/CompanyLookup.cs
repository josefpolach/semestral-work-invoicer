using Invoicing.App.Texts;
using Invoicing.App.ViewModels;
using Invoicing.Core.Companies;

namespace Invoicing.App.Services;

/// <summary>Applies successful ARES lookups to the shared address form.</summary>
internal class CompanyLookup(ICompanyRegistry registry, IDialogService dialogs)
{
    public async Task ApplyAsync(PartyForm form)
    {
        var result = await registry.FindByIcoAsync(form.Ico?.Trim() ?? string.Empty, CancellationToken.None);
        if (result.Company is CompanyInfo company)
        {
            form.Load(company);
        }
        else
        {
            await dialogs.ShowMessageAsync(CompanyLookupTexts.Format(result.Error));
        }
    }
}
