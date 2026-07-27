using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(120).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.GoogleId).HasMaxLength(64);
        builder.Property(u => u.EmailConfirmationCodeHash).HasMaxLength(128);
        builder.Property(u => u.PasswordResetTokenHash).HasMaxLength(128);
        builder.Property(u => u.TwoFactorSecret).HasMaxLength(200);
        builder.Property(u => u.TwoFactorChallengeTokenHash).HasMaxLength(128);
    }
}
