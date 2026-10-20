using AntigravitySaaS.Api.Domain.Entities;
using AntigravitySaaS.Api.Infrastructure.Data;
using AntigravitySaaS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AntigravitySaaS.Api.Services;

public class BillingService(SaasDbContext dbContext, ILogger<BillingService> logger) : IBillingService
{
    public async Task<bool> TryConsumeCreditAsync(Guid userId, int cost = 1, CancellationToken cancellationToken = default)
    {
        var newToken = Guid.NewGuid();

        // Update atômico sem tracking de memória
        var rowsAffected = await dbContext.Wallets
            .Where(w => w.UserId == userId && w.AvailableCredits >= cost)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(w => w.AvailableCredits, w => w.AvailableCredits - cost)
                .SetProperty(w => w.VersionToken, newToken), 
                cancellationToken);

        if (rowsAffected > 0)
        {
            logger.LogInformation("Débito atômico de {Cost} crédito(s) para o usuário {UserId}.", cost, userId);
            return true;
        }

        logger.LogWarning("Saldo insuficiente para o usuário {UserId}.", userId);
        return false;
    }

    public async Task RefundCreditAsync(Guid userId, int amount = 1, CancellationToken cancellationToken = default)
    {
        var newToken = Guid.NewGuid();

        var rowsAffected = await dbContext.Wallets
            .Where(w => w.UserId == userId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(w => w.AvailableCredits, w => w.AvailableCredits + amount)
                .SetProperty(w => w.VersionToken, newToken), 
                cancellationToken);

        if (rowsAffected > 0)
        {
            logger.LogInformation("Estorno de {Amount} crédito(s) realizado para o usuário {UserId}.", amount, userId);
        }
        else
        {
            logger.LogCritical("FALHA CRÍTICA: Carteira do usuário {UserId} não encontrada para estorno!", userId);
        }
    }

    public async Task<bool> AddCreditsAsync(Guid userId, int creditsToAdd, string stripePaymentId, CancellationToken cancellationToken = default)
    {
        // 1. Verificação de idempotência do webhook
        var alreadyProcessed = await dbContext.ProcessedPayments
            .AnyAsync(p => p.StripePaymentIntentId == stripePaymentId, cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogWarning("Evento Stripe {EventId} já processado. Ignorando duplicata.", stripePaymentId);
            return true;
        }

        // 2. Upsert da carteira
        var wallet = await dbContext.Wallets.FindAsync([userId], cancellationToken);
        if (wallet == null)
        {
            wallet = new UserWallet(userId, initialCredits: creditsToAdd);
            dbContext.Wallets.Add(wallet);
        }
        else
        {
            wallet.RefundCredits(creditsToAdd);
        }

        // 3. Registra o pagamento para idempotência
        dbContext.ProcessedPayments.Add(new ProcessedPayment
        {
            StripePaymentIntentId = stripePaymentId,
            UserId = userId,
            CreditsAdded = creditsToAdd
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Adicionados {Credits} créditos ao usuário {UserId}.", creditsToAdd, userId);
        return true;
    }

    public async Task LogGenerationAsync(Guid userId, string prompt, string imageUrl, int cost = 1, CancellationToken cancellationToken = default)
    {
        var log = new GenerationHistory
        {
            UserId = userId,
            OriginalPrompt = prompt,
            ImageUrl = imageUrl,
            CreditsConsumed = cost,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.GenerationHistories.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
