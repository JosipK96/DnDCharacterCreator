using CharacterCreatorApp.Domain.DomainModels.Races;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Races;

public class RaceLanguageConfiguration : IEntityTypeConfiguration<RaceLanguage>
{
    public void Configure(EntityTypeBuilder<RaceLanguage> builder)
    {
        builder.HasKey(e => new { e.RaceId, e.Language });

        builder.Property(e => e.RaceId)
            .HasMaxLength(50);

        builder.Property(e => e.Language)
            .HasMaxLength(100);

        builder.HasOne(e => e.Race)
            .WithMany(r => r.RaceLanguages)
            .HasForeignKey(e => e.RaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}