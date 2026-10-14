using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Languages;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Infrastructure.Identity;
using CharacterCreatorApp.Infrastructure.Seed;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using System.Reflection;

namespace CharacterCreatorApp.Infrastructure
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Class> Classes { get; set; }
        public DbSet<Proficiency> Proficiencies { get; set; }
        public DbSet<ClassProficiency> ClassProficiencies { get; set; }
        public DbSet<ProficiencyChoice> ProficiencyChoices { get; set; }
        public DbSet<ProficiencyChoiceOption> ProficiencyChoiceOptions { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<Subclass> Subclasses { get; set; }
        public DbSet<Race> Races { get; set; }
        public DbSet<RaceProficiency> RaceProficiencies { get; set; }
        public DbSet<Subrace> Subraces { get; set; }
        public DbSet<SubraceProficiency> SubraceProficiencies { get; set; }
        public DbSet<Background> Backgrounds { get; set; }
        public DbSet<BackgroundProficiency> BackgroundProficiencies { get; set; }
        public DbSet<Feat> Feats { get; set; }
        public DbSet<Spell> Spells { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<CharacterDraft> CharacterDrafts { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<RaceLanguage> RaceLanguages { get; set; }
        public DbSet<RaceAbilityBonus> RaceAbilityBonuses { get; set; }
        public DbSet<SubraceAbilityBonus> SubraceAbilityBonuses { get; set; }
        public DbSet<RaceTrait> RaceTraits { get; set; }
        public DbSet<SubraceTrait> SubraceTraits { get; set; }
        public DbSet<RaceSpell> RaceSpells { get; set; }
        public DbSet<SubraceSpell> SubraceSpells { get; set; }
        public DbSet<SpellClass> SpellClasses { get; set; }
        public DbSet<SpellSubclass> SpellSubclasses { get; set; }
        public DbSet<Spellcasting> Spellcastings { get; set; }
        public DbSet<ClassSpellProgression> ClassSpellProgressions { get; set; }
        public DbSet<SubclassSpellProgression> SubclassSpellProgressions { get; set; }
        public DbSet<ReferenceDataVersion> ReferenceDataVersions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityUserLogin<Guid>>(entity => entity.HasKey(e => new { e.LoginProvider, e.ProviderKey }));
            builder.Entity<IdentityUserRole<Guid>>(entity => entity.HasKey(e => new { e.UserId, e.RoleId }));
            builder.Entity<IdentityUserToken<Guid>>(entity => entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name }));

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
