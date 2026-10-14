using CharacterCreatorApp.Infrastructure.Seed;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.EntityConfigurations.Seed;

public class ReferenceDataVersionConfiguration : IEntityTypeConfiguration<ReferenceDataVersion>
{
    public void Configure(EntityTypeBuilder<ReferenceDataVersion> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedNever();

        // A SHA-256 in lowercase hex
        builder.Property(v => v.Hash)
            .HasMaxLength(64);
    }
}
