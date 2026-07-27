using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Category).HasMaxLength(80).IsRequired();
        builder.Property(b => b.PlannedAmount).HasColumnType("decimal(12,2)");

        builder.HasIndex(b => new { b.TripId, b.Category }).IsUnique();
    }
}
