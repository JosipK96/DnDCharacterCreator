using CharacterCreatorApp.Domain.DomainModels.Classes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Classes
{
    public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
    {
        public void Configure(EntityTypeBuilder<Feature> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Name)
                .HasMaxLength(50);

            builder.Property(f => f.Description);
            builder.Property(f => f.Level);

            builder.HasIndex(f => new { f.ClassId, f.Level });
            builder.HasIndex(f => f.SubclassId);

            builder.HasOne(f => f.Class)
                   .WithMany(c => c.Features)
                   .HasForeignKey(f => f.ClassId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Subclass)
                   .WithMany(s => s.Features)
                   .HasForeignKey(f => f.SubclassId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
