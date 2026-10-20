using System.Net.Http.Json;
using System.Text.Json;
using AntigravitySaaS.Api.Models;
using AntigravitySaaS.Api.Services.Interfaces;

namespace AntigravitySaaS.Api.Services;

public class AntigravityClient(HttpClient httpClient, ILogger<AntigravityClient> logger) : IAntigravityClient
{
    public async Task<string> GenerateImageAsync(AntigravityPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Disparando geração Antigravity na GPU. Sujeito: {Subject}", payload.DetectedSubject);

            var requestBody = new ProviderRequestEnvelope(
                Input: new ModelInput(
                    Prompt: payload.Prompt,
                    NegativePrompt: payload.NegativePrompt,
                    AspectRatio: payload.Parameters.AspectRatio,
                    GuidanceScale: payload.Parameters.GuidanceScale,
                    NumInferenceSteps: payload.Parameters.NumInferenceSteps
                )
            );

            using var request = new HttpRequestMessage(HttpMethod.Post, "predictions")
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("Prefer", "wait=60");

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ProviderResponse>(cancellationToken: cancellationToken);

            var finalUrl = result?.GetFirstOutputUrl();

            // Fallback de polling se o provedor estiver processando assincronamente
            if (string.IsNullOrEmpty(finalUrl) && result?.Status is "starting" or "processing" && !string.IsNullOrEmpty(result.Id))
            {
                finalUrl = await PollUntilCompletedAsync(result.Id, cancellationToken);
            }

            if (string.IsNullOrEmpty(finalUrl))
            {
                logger.LogWarning("API concluiu sem URL válida de saída. Status: {Status}", result?.Status);
                throw new InvalidOperationException($"Falha ao recuperar a imagem gerada. Status: {result?.Status}");
            }

            return finalUrl;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operação cancelada pelo chamador.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro na comunicação com a API de geração.");
            throw;
        }
    }

    private async Task<string?> PollUntilCompletedAsync(string predictionId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Aguardando job {Id} via polling...", predictionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var checkResponse = await httpClient.GetAsync($"predictions/{predictionId}", cancellationToken);
            checkResponse.EnsureSuccessStatusCode();

            var statusResult = await checkResponse.Content.ReadFromJsonAsync<ProviderResponse>(cancellationToken: cancellationToken);

            if (statusResult?.Status == "succeeded")
                return statusResult.GetFirstOutputUrl();

            if (statusResult?.Status is "failed" or "canceled")
                throw new InvalidOperationException($"Renderização falhou: {statusResult.Status}. Erro: {statusResult.Error}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return null;
    }
}
