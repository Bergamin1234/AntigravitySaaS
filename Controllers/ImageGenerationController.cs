using System.Text.RegularExpressions;
using AntigravitySaaS.Api.Extensions;
using AntigravitySaaS.Api.Models;
using AntigravitySaaS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;

namespace AntigravitySaaS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/generations")]
public partial class ImageGenerationController(
    IBillingService billingService,
    IPromptEngineService promptEngineService,
    IAntigravityClient antigravityClient,
    IImageStorageService imageStorageService,
    ILogger<ImageGenerationController> logger) : ControllerBase
{
    private const int GenerationCost = 1;

    [GeneratedRegex(@"^[a-zA-Z0-9\s.,!?'""\-áàãâéêíóôõúçÁÀÃÂÉÊÍÓÔÕÚÇ]+$")]
    private static partial Regex SafeInputRegex();

    [GeneratedRegex(@"\b(ignore|nsfw|bypass|nude|blood|hack)\b", RegexOptions.IgnoreCase)]
    private static partial Regex BlockedTermsRegex();

    [HttpPost("antigravity")]
    [ProducesResponseType(typeof(GenerateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateRequest request, 
        CancellationToken cancellationToken)
    {
        // 1. Extração segura do UserId via JWT
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { Error = "Token de autenticação inválido ou ausente." });

        // 2. Validações Estruturais e de Segurança
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { Error = "O prompt não pode estar vazio." });

        if (request.Prompt.Length > 120)
            return BadRequest(new { Error = "Limite de 120 caracteres excedido." });

        if (!SafeInputRegex().IsMatch(request.Prompt))
            return BadRequest(new { Error = "O prompt contém caracteres especiais não permitidos." });

        if (BlockedTermsRegex().IsMatch(request.Prompt))
        {
            logger.LogWarning("Tentativa de termo bloqueado: {Prompt}", request.Prompt);
            return BadRequest(new { Error = "O prompt viola as políticas de conteúdo." });
        }

        // 3. Débito atômico antecipado (Anti race-condition)
        var debitSuccess = await billingService.TryConsumeCreditAsync(userId, GenerationCost, cancellationToken);
        if (!debitSuccess)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, new 
            { 
                Error = "Saldo insuficiente. Adquira créditos para continuar gerando imagens." 
            });
        }

        var imageGenerated = false;

        try
        {
            // 4. Síntese do Prompt com a física Antigravity (0ms, Singleton)
            var payload = promptEngineService.BuildPayload(request.Prompt);

            // 5. Renderização na GPU com resiliência Polly
            var temporaryUrl = await antigravityClient.GenerateImageAsync(payload, cancellationToken);

            // 6. Persistência definitiva no Blob Storage
            var permanentUrl = await imageStorageService.TransferImageToPermanentStorageAsync(temporaryUrl, userId, cancellationToken);
            imageGenerated = true;

            // 7. Registro no Histórico do usuário
            await billingService.LogGenerationAsync(userId, request.Prompt, permanentUrl, GenerationCost, cancellationToken);

            return Ok(new GenerateResponse(permanentUrl, "Success", payload.DetectedSubject));
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuito de GPU aberto. Estornando crédito para {UserId}.", userId);
            await SafeRefundAsync(userId, GenerationCost);

            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new 
            { 
                Error = "Servidores de renderização temporariamente sobrecarregados. Seus créditos foram estornados. Tente em 30s." 
            });
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Requisição abortada pelo cliente. Estornando crédito...");
            await SafeRefundAsync(userId, GenerationCost);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro na renderização. Estornando crédito do usuário {UserId}.", userId);

            if (!imageGenerated)
            {
                await SafeRefundAsync(userId, GenerationCost);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                Error = "Instabilidade temporária na renderização. Seus créditos foram estornados automaticamente." 
            });
        }
    }

    private async Task SafeRefundAsync(Guid userId, int cost)
    {
        try
        {
            // CancellationToken.None é mandatório para que o estorno não seja cancelado se o cliente fechar a conexão!
            await billingService.RefundCreditAsync(userId, cost, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "ERRO CATASTRÓFICO: Falha no estorno de créditos para o usuário {UserId}!", userId);
        }
    }
}
