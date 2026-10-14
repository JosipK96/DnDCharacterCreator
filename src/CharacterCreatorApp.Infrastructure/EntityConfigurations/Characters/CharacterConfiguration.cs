using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Characters;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100);

        builder.Property(c => c.Edition)
            .HasMaxLength(4);

        builder.Property(c => c.RaceId)
            .HasMaxLength(50);

        builder.Property(c => c.SubraceId)
            .HasMaxLength(50);

        builder.Property(c => c.ClassId)
            .HasMaxLength(50);

        builder.Property(c => c.SubclassId)
            .HasMaxLength(50);

        builder.Property(c => c.BackgroundId)
            .HasMaxLength(50);

        // NoAction rather than Restrict: Postgres checks Restrict on the spot, but can defer a
        // NoAction check to the end of the transaction. SeedService does that when it replaces
        // the reference data, and the migration that made these keys NoAction also made them
        // deferrable, which EF has no setting for.
        builder.HasOne(c => c.Race)
            .WithMany()
            .HasForeignKey(c => c.RaceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Subrace)
            .WithMany()
            .HasForeignKey(c => c.SubraceId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(c => c.Class)
            .WithMany()
            .HasForeignKey(c => c.ClassId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Subclass)
            .WithMany()
            .HasForeignKey(c => c.SubclassId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasOne(c => c.Background)
            .WithMany()
            .HasForeignKey(c => c.BackgroundId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(c => c.AbilityScores)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        builder.Property(c => c.ProficiencyIds)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        builder.Property(c => c.LanguageIds)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        builder.Property(c => c.SpellIds)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        builder.Property(c => c.FeatIds)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        builder.Property(c => c.Size)
            .HasMaxLength(20);

        builder.Property(c => c.FeatSavingThrowAbilityIds)
            .HasColumnType("jsonb")
            .HasJsonConversion();

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.Name });
        builder.HasIndex(c => new { c.UserId, c.Level });
        builder.HasIndex(c => c.RaceId);
        builder.HasIndex(c => c.ClassId);
        builder.HasIndex(c => new { c.ClassId, c.Level });
        builder.HasIndex(c => c.Level);
        builder.HasIndex(x => x.CreatedAt);
    }
}