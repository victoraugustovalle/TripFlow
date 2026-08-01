using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripFlow.Domain.Entities;

namespace TripFlow.Infrastructure.Data.Configurations;

public class ItineraryProposalOptionConfiguration : IEntityTypeConfiguration<ItineraryProposalOption>
{
    public void Configure(EntityTypeBuilder<ItineraryProposalOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Title).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(2000);
        builder.Property(o => o.Location).HasMaxLength(300);

        builder.HasOne(o => o.ItineraryItem)
            .WithMany(i => i.ProposalOptions)
            .HasForeignKey(o => o.ItineraryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ItineraryVoteConfiguration : IEntityTypeConfiguration<ItineraryVote>
{
    public void Configure(EntityTypeBuilder<ItineraryVote> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.ItineraryItem).WithMany().HasForeignKey(v => v.ItineraryItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(v => v.Option).WithMany(o => o.Votes).HasForeignKey(v => v.OptionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(v => v.Participant).WithMany().HasForeignKey(v => v.ParticipantId).OnDelete(DeleteBehavior.Cascade);

        // Um voto por participante por PROPOSTA (nao por opcao) - trocar de opcao atualiza o
        // OptionId desse mesmo registro em vez de inserir um segundo voto.
        builder.HasIndex(v => new { v.ItineraryItemId, v.ParticipantId }).IsUnique();
    }
}
