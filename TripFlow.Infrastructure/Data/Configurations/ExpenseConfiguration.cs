using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(80);
        builder.Property(e => e.Amount).HasColumnType("decimal(12,2)");

        builder.HasOne(e => e.PaidByParticipant).WithMany().HasForeignKey(e => e.PaidByParticipantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Splits).WithOne(s => s.Expense).HasForeignKey(s => s.ExpenseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExpenseSplitConfiguration : IEntityTypeConfiguration<ExpenseSplit>
{
    public void Configure(EntityTypeBuilder<ExpenseSplit> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ShareAmount).HasColumnType("decimal(12,2)");

        builder.HasOne(s => s.Participant).WithMany(p => p.ExpenseSplits).HasForeignKey(s => s.ParticipantId).OnDelete(DeleteBehavior.Restrict);
    }
}
