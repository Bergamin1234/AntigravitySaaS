using System.Text.Json;
using System.Text.Json.Serialization;

namespace AntigravitySaaS.Api.Models;

public record GenerateRequest(string Prompt);
public record GenerateResponse(string ImageUrl, string Status, string Subject);

public record ProviderRequestEnvelope(
    [property: JsonPropertyName("input")] ModelInput Input
);

public record ModelInput(
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("negative_prompt")] string NegativePrompt,
    [property: JsonPropertyName("aspect_ratio")] string AspectRatio,
    [property: JsonPropertyName("guidance_scale")] float GuidanceScale,
    [property: JsonPropertyName("num_inference_steps")] int NumInferenceSteps
);

public record ProviderResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("output")] JsonElement OutputElement
)
{
    public string? GetFirstOutputUrl()
    {
        if (OutputElement.ValueKind == JsonValueKind.String)
            return OutputElement.GetString();

        if (OutputElement.ValueKind == JsonValueKind.Array && OutputElement.GetArrayLength() > 0)
            return OutputElement[0].GetString();

        return null;
    }
}
