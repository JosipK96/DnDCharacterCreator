using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class SubraceTraitConfiguration : IEntityTypeConfiguration<SubraceTrait>
{
    public void Configure(EntityTypeBuilder<SubraceTrait> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(100);

        builder.Property(e => e.SubraceId)
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.HasOne(e => e.Subrace)
            .WithMany(sr => sr.Traits)
            .HasForeignKey(e => e.SubraceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SubraceId);
    }
}