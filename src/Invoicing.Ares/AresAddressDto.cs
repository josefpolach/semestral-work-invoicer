using System.Text.Json.Serialization;

namespace Invoicing.Ares;

/// <summary>Fields used from the ARES address payload.</summary>
public class AresAddressDto
{
    [JsonPropertyName("nazevObce")]
    public string? City { get; set; }

    [JsonPropertyName("nazevCastiObce")]
    public string? District { get; set; }

    [JsonPropertyName("nazevUlice")]
    public string? Street { get; set; }

    [JsonPropertyName("cisloDomovni")]
    public int? HouseNumber { get; set; }

    [JsonPropertyName("cisloOrientacni")]
    public int? StreetNumber { get; set; }

    [JsonPropertyName("cisloOrientacniPismeno")]
    public string? StreetNumberSuffix { get; set; }

    [JsonPropertyName("psc")]
    public int? PostalCode { get; set; }

    [JsonPropertyName("textovaAdresa")]
    public string? FullAddress { get; set; }
}
