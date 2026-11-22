using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Invoicing.Core.Companies;
using Invoicing.Core.Validation;

namespace Invoicing.Ares;

/// <summary>Looks up Czech companies through the ARES REST API.</summary>
public class AresCompanyRegistry(HttpClient httpClient, string baseUrl) : ICompanyRegistry
{
    private const string SubjectPath = "/ekonomicke-subjekty/";
    private const string JsonMediaType = "application/json";

    public async Task<CompanyLookupResult> FindByIcoAsync(string ico, CancellationToken cancellationToken)
    {
        if (!IcoValidator.IsValid(ico))
        {
            return CompanyLookupResult.Failure(CompanyLookupError.InvalidIco);
        }

        try
        {
            return await RequestCompanyAsync(ico, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CompanyLookupResult.Failure(CompanyLookupError.Timeout);
        }
        catch (HttpRequestException)
        {
            return CompanyLookupResult.Failure(CompanyLookupError.NoConnection);
        }
        catch (JsonException)
        {
            return CompanyLookupResult.Failure(CompanyLookupError.UnexpectedResponse);
        }
    }

    private async Task<CompanyLookupResult> RequestCompanyAsync(string ico, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + SubjectPath + ico);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return CompanyLookupResult.Failure(CompanyLookupError.NotFound);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return CompanyLookupResult.Failure(CompanyLookupError.UnexpectedResponse);
        }

        var subject = await response.Content.ReadFromJsonAsync<AresSubjectDto>(cancellationToken);
        return subject?.Ico == ico && !string.IsNullOrWhiteSpace(subject.Name)
            ? CompanyLookupResult.Success(AresResponseMapper.Map(subject))
            : CompanyLookupResult.Failure(CompanyLookupError.UnexpectedResponse);
    }
}
