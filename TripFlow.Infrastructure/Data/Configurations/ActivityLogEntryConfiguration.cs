using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class ActivityLogEntryConfiguration : IEntityTypeConfiguration<ActivityLogEntry>
{
    public void Configure(EntityTypeBuilder<ActivityLogEntry> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Message).HasMaxLength(500).IsRequired();

        builder.HasOne(a => a.Trip).WithMany().HasForeignKey(a => a.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.ActorParticipant).WithMany().HasForeignKey(a => a.ActorParticipantId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.TripId, a.CreatedAt });
    }
}
