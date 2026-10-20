using AntigravitySaaS.Api.Models;
using AntigravitySaaS.Api.Services.Interfaces;

namespace AntigravitySaaS.Api.Services;

public class AntigravityPromptEngineService : IPromptEngineService
{
    private static readonly string[] FloatingEnvironments = 
    [
        "surrounded by fragmented architectural monoliths and levitating Escherian staircases leading to nowhere",
        "shattered reality of floating neoclassical marble ruins and geometric stone prisms suspended in deep space",
        "a dreamscape of levitating shattered mirrors reflecting alternate dimensions and floating monolithic pillars",
        "surrounded by an inverted celestial horizon with floating crystalline platforms drifting through cosmic dust"
    ];

    private static readonly string[] SurrealFluids = 
    [
        "suspended iridescent liquid mercury spheres orbiting in mid-air in defiance of physics",
        "luminescent fluid ribbons curling into floating helical spirals with weightless iridescent droplets",
        "floating bubbles of liquid gold reflecting distorted light and suspended aqueous tendrils",
        "antigravity water streams flowing upward into weightless floating spheres"
    ];

    private static readonly string[] CameraPerspectives = 
    [
        "captured from an extreme low-angle perspective looking upward to emphasize verticality, 35-degree Dutch angle tilt",
        "shot with an extreme wide-angle 14mm lens with subtle edge distortion, dramatic low-angle nadir shot",
        "cinematic dynamic low-angle composition with a vertiginous Dutch tilt, shallow depth of field",
        "towering low-angle framing looking up into infinity, wide-angle cinematic perspective"
    ];

    private static readonly string[] VolumetricLightings = 
    [
        "piercing volumetric god rays cutting through dense ethereal cosmic fog, intense cyan and amber rim lighting",
        "dramatic dual-tone rim lighting carving luminous sharp silhouettes against a deep void, prism caustics",
        "soft celestial bioluminescence combined with dramatic volumetric lighting beams and floating glowing embers",
        "ethereal twilight atmospheric haze with razor-sharp rim lights tracing edges of floating elements"
    ];

    private const string NegativePromptConstant = 
        "standing on ground, sitting on floor, normal gravity, static pose, flat horizon, mundane background, " +
        "studio portrait, eye-level shot, falling liquid, spilled water on floor, blurry, low resolution, bad anatomy, " +
        "deformed hands, extra limbs, watermark, signature, text, oversaturated, cartoon, 2d, sketch, ugly, tiling";

    private const string QualityTokensConstant = 
        "Octane Render, Unreal Engine 5, ray-traced subsurface scattering, hyper-detailed textures, 8k resolution, photorealistic surrealist masterpiece";

    public AntigravityPayload BuildPayload(string userInput)
    {
        var cleanInput = userInput.Trim();

        var environment = FloatingEnvironments[Random.Shared.Next(FloatingEnvironments.Length)];
        var fluid = SurrealFluids[Random.Shared.Next(SurrealFluids.Length)];
        var camera = CameraPerspectives[Random.Shared.Next(CameraPerspectives.Length)];
        var lighting = VolumetricLightings[Random.Shared.Next(VolumetricLightings.Length)];

        var masterPrompt = 
            $"A breathtaking surrealist masterpiece of ({cleanInput}:1.35) suspended weightlessly in mid-air in zero gravity, " +
            $"drifting in slow-motion bullet-time, {environment}, {fluid}, {camera}, {lighting}, {QualityTokensConstant}";

        return new AntigravityPayload(
            OriginalInput: cleanInput,
            DetectedSubject: cleanInput,
            Prompt: masterPrompt,
            NegativePrompt: NegativePromptConstant,
            Parameters: new RenderParameters(
                AspectRatio: "16:9",
                GuidanceScale: 8.0f,
                NumInferenceSteps: 40,
                StylePreset: "surreal-antigravity-v2"
            )
        );
    }
}
