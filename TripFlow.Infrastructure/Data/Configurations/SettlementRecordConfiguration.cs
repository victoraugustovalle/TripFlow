using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class SettlementRecordConfiguration : IEntityTypeConfiguration<SettlementRecord>
{
    public void Configure(EntityTypeBuilder<SettlementRecord> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Amount).HasColumnType("decimal(12,2)");

        builder.HasOne(s => s.Trip).WithMany().HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.FromParticipant).WithMany().HasForeignKey(s => s.FromParticipantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ToParticipant).WithMany().HasForeignKey(s => s.ToParticipantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.TripId, s.Status });
    }
}
