using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Characters
{
    public class CharacterDraftConfiguration : IEntityTypeConfiguration<CharacterDraft>
    {
        public void Configure(EntityTypeBuilder<CharacterDraft> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(100);

            builder.Property(x => x.Edition)
                .HasMaxLength(4);

            builder.Property(x => x.AllowedSources);

            builder.Property(x => x.RaceId)
                .HasMaxLength(50);

            builder.Property(x => x.SubraceId)
                .HasMaxLength(50);

            builder.Property(x => x.ClassId)
                .HasMaxLength(50);

            builder.Property(x => x.SubclassId)
                .HasMaxLength(50);

            builder.Property(x => x.BackgroundId)
                .HasMaxLength(50);

            builder.Property(x => x.BaseAbilityScores)
                .HasColumnType("jsonb")
                .HasJsonConversion();

            builder.Property(x => x.RaceAbilityIncreases)
                .HasColumnType("jsonb")
                .HasJsonConversion();

            builder.Property(x => x.BackgroundAbilityIncreases)
                .HasColumnType("jsonb")
                .HasJsonConversion();

            builder.Property(x => x.SelectedClassProficiencyIds);
            builder.Property(x => x.SelectedRaceSkillIds);
            builder.Property(x => x.SelectedLanguageIds);
            builder.Property(x => x.SelectedFeatIds);
            builder.Property(x => x.FightingStyleIds);
            builder.Property(x => x.SelectedSpellIds);

            builder.Property(x => x.FeatAbilityIncreases)
                .HasColumnType("jsonb")
                .HasJsonConversion();

            builder.Property(x => x.AbilityScoreImprovements)
                .HasColumnType("jsonb")
                .HasJsonConversion();

            builder.HasIndex(x => x.CreatedByUserId);
            builder.HasIndex(x => new { x.CreatedByUserId, x.IsCompleted });
            builder.HasIndex(x => x.CreatedAt);
        }
    }

}
