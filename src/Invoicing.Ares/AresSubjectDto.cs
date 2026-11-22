using System.Text.Json.Serialization;

namespace Invoicing.Ares;

/// <summary>Fields used from the ARES subject payload.</summary>
public class AresSubjectDto
{
    [JsonPropertyName("ico")]
    public string? Ico { get; set; }

    [JsonPropertyName("obchodniJmeno")]
    public string? Name { get; set; }

    [JsonPropertyName("dic")]
    public string? Dic { get; set; }

    [JsonPropertyName("sidlo")]
    public AresAddressDto? Address { get; set; }
}
