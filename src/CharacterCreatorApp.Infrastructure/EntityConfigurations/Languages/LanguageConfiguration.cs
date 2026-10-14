using CharacterCreatorApp.Domain.DomainModels.Languages;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Languages;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasMaxLength(100);

        builder.Property(e => e.Name)
            .HasMaxLength(100);

        builder.Property(e => e.Type)
            .HasMaxLength(20);

        builder.Property(e => e.Script)
            .HasMaxLength(50);

        builder.Property(e => e.Source)
            .HasMaxLength(20);
    }
}