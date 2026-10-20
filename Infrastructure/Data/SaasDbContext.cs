using AntigravitySaaS.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AntigravitySaaS.Api.Infrastructure.Data;

public class SaasDbContext(DbContextOptions<SaasDbContext> options) : DbContext(options)
{
    public DbSet<UserWallet> Wallets => Set<UserWallet>();
    public DbSet<GenerationHistory> GenerationHistories => Set<GenerationHistory>();
    public DbSet<ProcessedPayment> ProcessedPayments => Set<ProcessedPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Configuração da Carteira (UserWallet)
        modelBuilder.Entity<UserWallet>(entity =>
        {
            entity.HasKey(w => w.UserId);

            entity.Property(w => w.VersionToken)
                .IsConcurrencyToken()
                .IsRequired();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_UserWallets_AvailableCredits_NonNegative", 
                "\"AvailableCredits\" >= 0"));
        });

        // 2. Configuração do Histórico de Gerações
        modelBuilder.Entity<GenerationHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.OriginalPrompt)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(h => h.ImageUrl)
                .HasMaxLength(2048)
                .IsRequired();

            // Índice composto para paginação rápida da galeria do usuário
            entity.HasIndex(h => new { h.UserId, h.CreatedAt })
                .HasDatabaseName("IX_GenerationHistories_UserId_CreatedAt");
        });

        // 3. Pagamentos processados (Idempotência da Stripe)
        modelBuilder.Entity<ProcessedPayment>(entity =>
        {
            entity.HasKey(p => p.StripePaymentIntentId);
            entity.HasIndex(p => p.UserId);
        });
    }
}
