using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class ProactiveNudgeLogConfiguration : IEntityTypeConfiguration<ProactiveNudgeLog>
{
    public void Configure(EntityTypeBuilder<ProactiveNudgeLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Key).HasMaxLength(120).IsRequired();

        builder.HasOne(l => l.Trip).WithMany().HasForeignKey(l => l.TripId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.TripId, l.Key }).IsUnique();
    }
}
