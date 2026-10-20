using System.Text.Json.Serialization;

namespace AntigravitySaaS.Api.Models;

public record AntigravityPayload(
    [property: JsonPropertyName("original_input")] string OriginalInput,
    [property: JsonPropertyName("detected_subject")] string DetectedSubject,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("negative_prompt")] string NegativePrompt,
    [property: JsonPropertyName("parameters")] RenderParameters Parameters
);

public record RenderParameters(
    [property: JsonPropertyName("aspect_ratio")] string AspectRatio,
    [property: JsonPropertyName("guidance_scale")] float GuidanceScale,
    [property: JsonPropertyName("num_inference_steps")] int NumInferenceSteps,
    [property: JsonPropertyName("style_preset")] string StylePreset
);
