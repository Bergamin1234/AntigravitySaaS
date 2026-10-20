using System.ComponentModel.DataAnnotations;

namespace AntigravitySaaS.Api.Domain.Entities;

public class UserWallet
{
    [Key]
    public Guid UserId { get; private set; }
    
    public int AvailableCredits { get; private set; }

    [ConcurrencyCheck]
    public Guid VersionToken { get; private set; }

    // Construtor para o EF Core
    private UserWallet() { }

    public UserWallet(Guid userId, int initialCredits)
    {
        UserId = userId;
        AvailableCredits = initialCredits;
        VersionToken = Guid.NewGuid();
    }

    public void DeductCredits(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("A quantidade a ser debitada deve ser maior que zero.", nameof(amount));

        if (AvailableCredits < amount)
            throw new InvalidOperationException("Saldo insuficiente de créditos.");

        AvailableCredits -= amount;
        VersionToken = Guid.NewGuid(); // Rotaciona o token em toda alteração
    }

    public void RefundCredits(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("A quantidade a ser estornada deve ser maior que zero.", nameof(amount));

        AvailableCredits += amount;
        VersionToken = Guid.NewGuid(); // Rotaciona o token também no estorno/depósito
    }
}

public class GenerationHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public string OriginalPrompt { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int CreditsConsumed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ProcessedPayment
{
    [Key]
    public string StripePaymentIntentId { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    public int CreditsAdded { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
