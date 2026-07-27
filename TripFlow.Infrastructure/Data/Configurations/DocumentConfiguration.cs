using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(500).IsRequired();

        builder.HasOne(d => d.UploadedByParticipant)
            .WithMany()
            .HasForeignKey(d => d.UploadedByParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
