using Application.Common.Interfaces;
using Domain.Auth;
using Domain.Catalog;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class GameVaultDbContext(DbContextOptions<GameVaultDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TokenHash)
                .HasMaxLength(44)
                .IsRequired();
            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();

            entity.Property(x => x.DeviceId).HasMaxLength(256);
            entity.Property(x => x.UserAgent).HasMaxLength(512);

            entity.HasIndex(x => x.UserId);
        });
    }
}
