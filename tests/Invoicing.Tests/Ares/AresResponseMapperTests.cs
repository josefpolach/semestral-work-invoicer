using System.Text.Json;
using Invoicing.Ares;
using Invoicing.Core.Companies;

namespace Invoicing.Tests.Ares;

public class AresResponseMapperTests
{
    [Fact]
    public async Task Map_CompleteResponse_MapsCompanyDetails()
    {
        var subject = await LoadSubjectAsync();
        Assert.Equal(new CompanyInfo("27074358", "Asseco Central Europe, a.s.", "Budějovická 778/3a", "Praha",
            "14000", "CZ27074358"), AresResponseMapper.Map(subject));
    }

    [Fact]
    public async Task Map_MissingTaxNumber_ReturnsNull()
    {
        var subject = await LoadSubjectAsync();
        subject.Dic = null;
        Assert.Null(AresResponseMapper.Map(subject).Dic);
    }

    [Fact]
    public async Task Map_MissingStreet_UsesDistrict()
    {
        var subject = await LoadSubjectAsync();
        var address = Assert.IsType<AresAddressDto>(subject.Address);
        address.Street = null;
        Assert.Equal("Michle 778/3a", AresResponseMapper.Map(subject).Street);
    }

    [Fact]
    public async Task Map_MissingPostalCode_UsesFullAddressAndLeavesCityEmpty()
    {
        var subject = await LoadSubjectAsync();
        var address = Assert.IsType<AresAddressDto>(subject.Address);
        address.PostalCode = null;
        var result = AresResponseMapper.Map(subject);
        Assert.Equal(address.FullAddress, result.Street);
        Assert.Empty(result.City);
        Assert.Empty(result.PostalCode);
    }

    private static async Task<AresSubjectDto> LoadSubjectAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "ares-27074358.json");
        return Assert.IsType<AresSubjectDto>(JsonSerializer.Deserialize<AresSubjectDto>(await File.ReadAllTextAsync(path)));
    }
}
