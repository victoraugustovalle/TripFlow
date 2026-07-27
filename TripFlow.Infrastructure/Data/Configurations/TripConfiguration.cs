using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Destination).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.Currency).HasMaxLength(3);

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Participants).WithOne(p => p.Trip).HasForeignKey(p => p.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.Expenses).WithOne(e => e.Trip).HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.Budgets).WithOne(b => b.Trip).HasForeignKey(b => b.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.ChecklistItems).WithOne(c => c.Trip).HasForeignKey(c => c.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.ItineraryItems).WithOne(i => i.Trip).HasForeignKey(i => i.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.Reservations).WithOne(r => r.Trip).HasForeignKey(r => r.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.Documents).WithOne(d => d.Trip).HasForeignKey(d => d.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}
