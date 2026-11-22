using System.Net;
using System.Text.Json;
using Invoicing.Ares;
using Invoicing.Core.Companies;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Ares;

public class AresCompanyRegistryTests
{
    private const string BaseUrl = "https://example.com/ares/rest";
    private const string Ico = "27074358";

    [Fact]
    public async Task FindByIcoAsync_ValidFixture_RequestsFullPathAndMapsAddress()
    {
        var json = await LoadFixtureAsync();
        using var handler = new FakeHttpMessageHandler(() => FakeHttpMessageHandler.WithBody(HttpStatusCode.OK, json));
        using var client = new HttpClient(handler);
        var result = await new AresCompanyRegistry(client, BaseUrl).FindByIcoAsync(Ico, CancellationToken.None);
        Assert.Equal(new CompanyInfo(Ico, "Asseco Central Europe, a.s.", "Budějovická 778/3a", "Praha", "14000",
            "CZ27074358"), result.Company);
        Assert.Equal(BaseUrl + "/ekonomicke-subjekty/27074358", handler.Url?.AbsoluteUri);
        Assert.Equal("application/json", handler.Headers["Accept"]);
    }

    [Fact]
    public async Task Map_MissingFields_UsesDistrictAndFullAddressFallbacks()
    {
        var subject = Assert.IsType<AresSubjectDto>(JsonSerializer.Deserialize<AresSubjectDto>(await LoadFixtureAsync()));
        var address = Assert.IsType<AresAddressDto>(subject.Address);
        subject.Dic = null;
        address.Street = null;
        var result = AresResponseMapper.Map(subject);
        Assert.Equal("Michle 778/3a", result.Street);
        Assert.Null(result.Dic);
        address.PostalCode = null;
        result = AresResponseMapper.Map(subject);
        Assert.Equal(address.FullAddress, result.Street);
        Assert.Empty(result.City);
        Assert.Empty(result.PostalCode);
    }

    [Theory]
    [InlineData("invalid", CompanyLookupError.InvalidIco, 0)]
    [InlineData("27074358", CompanyLookupError.NotFound, 1)]
    [InlineData("27074358", CompanyLookupError.NoConnection, 1)]
    [InlineData("27074358", CompanyLookupError.Timeout, 1)]
    public async Task FindByIcoAsync_InvalidInputOrProviderFailure_ReturnsCode(string ico, CompanyLookupError code, int calls)
    {
        using var handler = new FakeHttpMessageHandler(() => code switch
        {
            CompanyLookupError.NoConnection => throw new HttpRequestException(),
            CompanyLookupError.Timeout => throw new TaskCanceledException(),
            _ => new(HttpStatusCode.NotFound)
        });
        using var client = new HttpClient(handler);
        var result = await new AresCompanyRegistry(client, BaseUrl).FindByIcoAsync(ico, CancellationToken.None);
        Assert.Equal(code, result.Error);
        Assert.Equal(calls, handler.Calls);
    }

    private static Task<string> LoadFixtureAsync() =>
        File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "ares-27074358.json"));
}
