using Domain.Auth;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class GameVaultDbContext(DbContextOptions<GameVaultDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TokenHash)
                .HasMaxLength(256)
                .IsRequired();
            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.Property(x => x.DeviceId).HasMaxLength(256);
            entity.Property(x => x.UserAgent).HasMaxLength(512);

            entity.HasIndex(x => x.UserId);
        });
    }
}
