using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class TripMemoryConfiguration : IEntityTypeConfiguration<TripMemory>
{
    public void Configure(EntityTypeBuilder<TripMemory> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Highlight).HasMaxLength(500);

        builder.HasOne(m => m.Trip).WithMany().HasForeignKey(m => m.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Participant).WithMany().HasForeignKey(m => m.ParticipantId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.TripId, m.ParticipantId }).IsUnique();
    }
}
