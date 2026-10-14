using CharacterCreatorApp.Domain.DomainModels.Feats;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Feats
{
    public class FeatConfiguration : IEntityTypeConfiguration<Feat>
    {
        public void Configure(EntityTypeBuilder<Feat> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Name);
            builder.Property(f => f.Description);
            builder.Property(f => f.Edition).HasMaxLength(4);
            builder.Property(f => f.Category).HasMaxLength(30);
            builder.HasIndex(f => f.Edition);
            builder.Property(f => f.IsRepeatable);
            builder.Property(f => f.GrantedProficiencyIds);
            builder.Property(f => f.GrantedLanguageIds);
            builder.Property(f => f.GrantedSpellIds);

            builder.OwnsOne(f => f.Prerequisites, prerequisites =>
            {
                prerequisites.ToJson();
                prerequisites.OwnsMany(p => p.AbilityMinimums);
            });
            builder.Navigation(f => f.Prerequisites).IsRequired();

            builder.OwnsOne(f => f.Choice).ToJson();
            builder.OwnsOne(f => f.AbilityIncrease).ToJson();
        }
    }
}
