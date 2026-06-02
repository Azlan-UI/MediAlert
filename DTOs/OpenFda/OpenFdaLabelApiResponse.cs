using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediAlert.DTOs.OpenFda;

public sealed class OpenFdaLabelApiResponse
{
    [JsonPropertyName("meta")]
    public OpenFdaMeta? Meta { get; set; }

    [JsonPropertyName("results")]
    public List<OpenFdaLabelResult>? Results { get; set; }
}

public sealed class OpenFdaMeta
{
    [JsonPropertyName("results")]
    public OpenFdaMetaResults? Results { get; set; }
}

public sealed class OpenFdaMetaResults
{
    [JsonPropertyName("skip")]
    public int? Skip { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }
}

public sealed class OpenFdaLabelResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("set_id")]
    public string? SetId { get; set; }

    [JsonPropertyName("effective_time")]
    public string? EffectiveTime { get; set; }

    [JsonPropertyName("openfda")]
    public OpenFdaFields? OpenFda { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    [JsonPropertyName("boxed_warning")]
    public List<string>? BoxedWarnings { get; set; }

    [JsonPropertyName("contraindications")]
    public List<string>? Contraindications { get; set; }

    [JsonPropertyName("drug_interactions")]
    public List<string>? DrugInteractions { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed class OpenFdaFields
{
    [JsonPropertyName("brand_name")]
    public List<string>? BrandName { get; set; }

    [JsonPropertyName("generic_name")]
    public List<string>? GenericName { get; set; }

    [JsonPropertyName("manufacturer_name")]
    public List<string>? ManufacturerName { get; set; }

    [JsonPropertyName("product_type")]
    public List<string>? ProductType { get; set; }

    [JsonPropertyName("route")]
    public List<string>? Route { get; set; }

    [JsonPropertyName("substance_name")]
    public List<string>? SubstanceName { get; set; }

    [JsonPropertyName("rxcui")]
    public List<string>? RxCui { get; set; }

    [JsonPropertyName("product_ndc")]
    public List<string>? ProductNdc { get; set; }

    [JsonPropertyName("package_ndc")]
    public List<string>? PackageNdc { get; set; }
}
