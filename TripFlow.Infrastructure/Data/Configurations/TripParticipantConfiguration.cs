using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class TripParticipantConfiguration : IEntityTypeConfiguration<TripParticipant>
{
    public void Configure(EntityTypeBuilder<TripParticipant> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.InvitedEmail).HasMaxLength(256).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(120);
        builder.Property(p => p.InviteTokenHash).HasMaxLength(128);

        builder.HasIndex(p => new { p.TripId, p.InvitedEmail }).IsUnique();

        builder.HasOne(p => p.User).WithMany(u => u.TripParticipations).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
