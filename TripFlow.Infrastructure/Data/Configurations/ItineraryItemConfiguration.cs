using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class ItineraryItemConfiguration : IEntityTypeConfiguration<ItineraryItem>
{
    public void Configure(EntityTypeBuilder<ItineraryItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.Location).HasMaxLength(300);

        builder.HasMany(i => i.Reservations)
            .WithOne(r => r.ItineraryItem)
            .HasForeignKey(r => r.ItineraryItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
