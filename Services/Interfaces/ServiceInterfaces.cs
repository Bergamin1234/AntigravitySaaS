using AntigravitySaaS.Api.Models;

namespace AntigravitySaaS.Api.Services.Interfaces;

public interface IAntigravityClient
{
    Task<string> GenerateImageAsync(AntigravityPayload payload, CancellationToken cancellationToken = default);
}

public interface IBillingService
{
    Task<bool> TryConsumeCreditAsync(Guid userId, int cost = 1, CancellationToken cancellationToken = default);
    Task RefundCreditAsync(Guid userId, int amount = 1, CancellationToken cancellationToken = default);
    Task<bool> AddCreditsAsync(Guid userId, int creditsToAdd, string stripePaymentId, CancellationToken cancellationToken = default);
    Task LogGenerationAsync(Guid userId, string prompt, string imageUrl, int cost = 1, CancellationToken cancellationToken = default);
}

public interface IPromptEngineService
{
    AntigravityPayload BuildPayload(string userInput);
}

public interface IImageStorageService
{
    Task<string> TransferImageToPermanentStorageAsync(string temporaryAiUrl, Guid userId, CancellationToken cancellationToken = default);
}
