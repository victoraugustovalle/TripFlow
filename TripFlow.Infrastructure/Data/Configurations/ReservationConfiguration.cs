using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ProviderName).HasMaxLength(200);
        builder.Property(r => r.ConfirmationCode).HasMaxLength(80);
        builder.Property(r => r.Location).HasMaxLength(300);
        builder.Property(r => r.Currency).HasMaxLength(3);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.Price).HasColumnType("decimal(12,2)");
    }
}
