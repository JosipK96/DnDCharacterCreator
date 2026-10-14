using CharacterCreatorApp.Domain.DomainModels.Backgrounds;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Backgrounds
{
    public class BackgroundConfiguration : IEntityTypeConfiguration<Background>
    {
        public void Configure(EntityTypeBuilder<Background> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                .HasMaxLength(50);

            builder.Property(b => b.Name)
                .HasMaxLength(50);

            builder.Property(b => b.Edition)
                .HasMaxLength(4);

            builder.Property(b => b.Languages);
            builder.Property(b => b.AbilityScoreOptions);

            builder.HasIndex(b => b.Edition);

            builder.HasMany(b => b.BackgroundProficiencies)
                .WithOne(bp => bp.Background)
                .HasForeignKey(bp => bp.BackgroundId)
                .OnDelete(DeleteBehavior.Cascade);

            // A 2014 background's feature; the reference data sync deletes features first, and
            // their backgrounds go with them
            builder.HasOne(b => b.Feature)
                .WithOne()
                .HasForeignKey<Background>(b => b.FeatureId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // The sync deletes backgrounds before feats, so a feat is never deleted from under one
            builder.HasOne(b => b.Feat)
                .WithMany()
                .HasForeignKey(b => b.FeatId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
