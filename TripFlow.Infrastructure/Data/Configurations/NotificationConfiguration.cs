using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();

        builder.HasOne(n => n.Trip).WithMany().HasForeignKey(n => n.TripId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(n => n.RecipientUser).WithMany().HasForeignKey(n => n.RecipientUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
    }
}

public class NotificationMuteConfiguration : IEntityTypeConfiguration<NotificationMute>
{
    public void Configure(EntityTypeBuilder<NotificationMute> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Trip).WithMany().HasForeignKey(m => m.TripId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.UserId, m.TripId, m.Type }).IsUnique();
    }
}
