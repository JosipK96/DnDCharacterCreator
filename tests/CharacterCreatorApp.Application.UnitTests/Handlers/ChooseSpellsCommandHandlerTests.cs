using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseSpells;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class ChooseSpellsCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly ChooseSpellsCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public ChooseSpellsCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new ChooseSpellsCommandHandler(
            _draftRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _userContextMock.Object,
            _unitOfWorkMock.Object,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Handle_WithTheCantripsAndSpellsTheTableGives_SavesThem()
    {
        // Arrange - a level 1 sorcerer knows four cantrips and two level 1 spells
        var draft = ArrangeDraft("sorcerer", level: 1);
        ArrangeClass("sorcerer", [Row("sorcerer", 1, cantrips: 4, spells: 2, maxSpellLevel: 1)]);
        ArrangeSpellList("sorcerer", Cantrip("fire_bolt"), Cantrip("light"), Cantrip("mage_hand"), Cantrip("prestidigitation"),
            Spell("shield", 1), Spell("magic_missile", 1));

        // Act
        var result = await _sut.Handle(
            new ChooseSpellsCommand(_draftId, ["fire_bolt", "light", "mage_hand", "prestidigitation", "shield", "magic_missile"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedSpellIds.Should().Equal("fire_bolt", "light", "mage_hand", "prestidigitation", "shield", "magic_missile");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithASpellFromAnotherClassList_ReturnsInvalidSpell()
    {
        // Arrange - cure_wounds is a cleric spell, not on the wizard list
        var draft = ArrangeDraft("wizard", level: 1);
        ArrangeClass("wizard", [Row("wizard", 1, cantrips: 0, spells: 2, maxSpellLevel: 1)]);
        ArrangeSpellList("wizard", Spell("shield", 1));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["shield", "cure_wounds"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.Invalid");
    }

    [Fact]
    public async Task Handle_WithTooFewCantrips_ReturnsCantripCountMismatch()
    {
        // Arrange - a level 1 sorcerer picks four cantrips
        var draft = ArrangeDraft("sorcerer", level: 1);
        ArrangeClass("sorcerer", [Row("sorcerer", 1, cantrips: 4, spells: 0, maxSpellLevel: 1)]);
        ArrangeSpellList("sorcerer", Cantrip("fire_bolt"), Cantrip("light"), Cantrip("mage_hand"), Cantrip("prestidigitation"));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["fire_bolt", "light"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.CantripCountMismatch");
    }

    [Theory]
    [InlineData("cleric", "Wisdom", 1, 1, 16, 4)]
    [InlineData("druid", "Wisdom", 1, 3, 14, 5)]
    [InlineData("paladin", "Charisma", 2, 5, 16, 5)]
    [InlineData("artificer", "Intelligence", 2, 3, 16, 4)]
    public async Task Handle_ForA2014PreparedCasterWithAsManySpellsAsItPrepares_SavesThem(
        string classId, string ability, int divisor, int level, int score, int prepared)
    {
        // Arrange - its spellcasting modifier plus its level, half of it for a paladin or
        // artificer, from the scores as the sheet has them: the race's +2 counts
        var draft = ArrangePreparedCasterDraft(classId, ability, level, score - 2, raceBonus: 2);
        ArrangePreparedCaster(classId, ability, divisor, level);
        ArrangeSpellList(classId, LevelOneSpells(8));
        List<string> spellIds = [.. LevelOneSpells(prepared).Select(s => s.Id)];

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, spellIds), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Description)));
        draft.SelectedSpellIds.Should().Equal(spellIds);
    }

    [Theory]
    [InlineData("cleric", "Wisdom", 1, 1, 16, 5)]
    [InlineData("cleric", "Wisdom", 1, 1, 16, 3)]
    [InlineData("druid", "Wisdom", 1, 3, 14, 6)]
    [InlineData("druid", "Wisdom", 1, 3, 14, 0)]
    [InlineData("paladin", "Charisma", 2, 5, 16, 6)]
    [InlineData("paladin", "Charisma", 2, 5, 16, 4)]
    [InlineData("artificer", "Intelligence", 2, 3, 16, 5)]
    [InlineData("artificer", "Intelligence", 2, 3, 16, 3)]
    public async Task Handle_ForA2014PreparedCasterWithTooManyOrTooFewSpells_ReturnsSpellCountMismatch(
        string classId, string ability, int divisor, int level, int score, int picked)
    {
        // Arrange - one more or fewer than its modifier plus its level (or half of it)
        var draft = ArrangePreparedCasterDraft(classId, ability, level, score, raceBonus: 0);
        ArrangePreparedCaster(classId, ability, divisor, level);
        ArrangeSpellList(classId, LevelOneSpells(8));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, [.. LevelOneSpells(picked).Select(s => s.Id)]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.SpellCountMismatch");
    }

    [Fact]
    public async Task Handle_ForA2014PreparedCasterWithASpellAboveItsLevel_ReturnsLevelTooHigh()
    {
        // Arrange - a level 1 cleric with WIS 12 prepares two spells, of level 1 only
        var draft = ArrangePreparedCasterDraft("cleric", "Wisdom", 1, 12, raceBonus: 0);
        ArrangePreparedCaster("cleric", "Wisdom", 1, 1);
        ArrangeSpellList("cleric", Spell("bless", 1), Spell("lesser_restoration", 2));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["bless", "lesser_restoration"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.LevelTooHigh");
    }

    [Fact]
    public async Task Handle_ForA2014PreparedCasterBeforeItsScoresAreChosen_ReturnsScoresRequired()
    {
        // Arrange - the count follows the final scores, which a draft without them cannot give
        var draft = ArrangeDraft("cleric", level: 1);
        ArrangePreparedCaster("cleric", "Wisdom", 1, 1);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["bless"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.ScoresRequired");
        _raceRepositoryMock.Verify(r => r.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTooManySpells_ReturnsSpellCountMismatch()
    {
        // Arrange - a level 1 warlock knows two spells
        var draft = ArrangeDraft("warlock", level: 1);
        ArrangeClass("warlock", [Row("warlock", 1, cantrips: 0, spells: 2, maxSpellLevel: 1)]);
        ArrangeSpellList("warlock", Spell("hex", 1), Spell("armor_of_agathys", 1), Spell("witch_bolt", 1));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["hex", "armor_of_agathys", "witch_bolt"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.SpellCountMismatch");
    }

    [Fact]
    public async Task Handle_WithASpellAboveTheHighestLevel_ReturnsLevelTooHigh()
    {
        // Arrange - a level 2 bard casts up to level 1, so Hold Person is not yet its to pick
        var draft = ArrangeDraft("bard", level: 2);
        ArrangeClass("bard", [Row("bard", 2, cantrips: 0, spells: 2, maxSpellLevel: 1)]);
        ArrangeSpellList("bard", Spell("healing_word", 1), Spell("hold_person", 2));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["healing_word", "hold_person"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.LevelTooHigh");
    }

    [Fact]
    public async Task Handle_WithTheSameSpellTwice_ReturnsDuplicate()
    {
        // Arrange - two picks of one spell would make the count without a second spell
        var draft = ArrangeDraft("warlock", level: 1);
        ArrangeClass("warlock", [Row("warlock", 1, cantrips: 0, spells: 2, maxSpellLevel: 1)]);
        ArrangeSpellList("warlock", Spell("hex", 1), Spell("armor_of_agathys", 1));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["hex", "hex"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.Duplicate");
    }

    [Fact]
    public async Task Handle_ForACharacterThatCastsNoSpellsYet_ReturnsNoSpellcasting()
    {
        // Arrange - a 2014 paladin's table starts at level 2
        var draft = ArrangeDraft("paladin", level: 1);
        ArrangeClass("paladin", [Row("paladin", 2, cantrips: 0, spells: 0, maxSpellLevel: 1)]);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["bless"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.NoSpellcasting");
        _spellRepositoryMock.Verify(r => r.GetByClassIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForACharacterThatCastsNoSpellsYetPickingNone_SavesTheEmptyList()
    {
        // Arrange - a fighter without the Eldritch Knight has nothing to pick, which is a valid pick
        var draft = ArrangeDraft("fighter", level: 1);
        ArrangeClass("fighter", []);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedSpellIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EldritchKnightWithWizardSpells_SavesThem()
    {
        // Arrange - the fighter has no list or table; the subclass opens the wizard's list and
        // has its own table, both of which used to be left out
        var draft = ArrangeDraft("fighter", level: 3, subclassId: "eldritch-knight");
        ArrangeClass("fighter", [], new Subclass
        {
            Id = "eldritch-knight",
            Name = "Eldritch Knight",
            Source = "PHB",
            ClassId = "fighter",
            SpellListClassId = "wizard",
            SpellProgression =
            [
                new SubclassSpellProgression
                {
                    SubclassId = "eldritch-knight", CharacterLevel = 3, CantripsKnown = 1, SpellsKnown = 2, MaxSpellLevel = 1, SpellcastingAbility = "INT"
                }
            ]
        });
        ArrangeSpellList("fighter");
        ArrangeSpellList("wizard", Cantrip("fire_bolt"), Spell("shield", 1), Spell("magic_missile", 1));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["fire_bolt", "shield", "magic_missile"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedSpellIds.Should().Equal("fire_bolt", "shield", "magic_missile");
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(8, 2)]
    [InlineData(14, 3)]
    public async Task Handle_EldritchKnightWithOneOutsideSpellPerAnySchoolLevel_SavesThem(int level, int outsideSpells)
    {
        // Arrange - one spell from any school at each of levels 3, 8, 14 and 20; the rest
        // abjuration or evocation, and a conjuration cantrip, since cantrips are not limited
        var draft = ArrangeDraft("fighter", level, subclassId: "eldritch-knight");
        ArrangeClass("fighter", [], SchoolLimitedSubclass("eldritch-knight", "fighter", ["Abjuration", "Evocation"], level, spellsKnown: 4));
        ArrangeSpellList("fighter");
        List<string> spellIds = ["mage_hand", .. Enumerable.Range(1, outsideSpells).Select(i => $"outside_{i}"), .. Enumerable.Range(1, 4 - outsideSpells).Select(i => $"inside_{i}")];
        ArrangeSpellList("wizard", [Spell("mage_hand", 0, "Conjuration"), .. SchoolSpells(outsideSpells, 4 - outsideSpells, "Evocation")]);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, spellIds), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedSpellIds.Should().Equal(spellIds);
    }

    [Theory]
    [InlineData("eldritch-knight", "fighter", "Abjuration", "Evocation", 3, 2)]
    [InlineData("eldritch-knight", "fighter", "Abjuration", "Evocation", 8, 3)]
    [InlineData("arcane-trickster", "rogue", "Enchantment", "Illusion", 3, 2)]
    public async Task Handle_WithMoreSpellsOutsideTheSubclassSchoolsThanItsAnySchoolPicks_ReturnsSchoolNotAllowed(
        string subclassId, string classId, string school, string otherSchool, int level, int outsideSpells)
    {
        // Arrange - a 2014 Eldritch Knight or Arcane Trickster picks from the whole wizard list,
        // but only one spell per level 3, 8, 14 and 20 from outside its two schools
        var draft = ArrangeDraft(classId, level, subclassId);
        ArrangeClass(classId, [], SchoolLimitedSubclass(subclassId, classId, [school, otherSchool], level, spellsKnown: 4));
        ArrangeSpellList(classId);
        ArrangeSpellList("wizard", [Cantrip("fire_bolt"), .. SchoolSpells(outsideSpells, 4 - outsideSpells, school)]);
        List<string> spellIds = ["fire_bolt", .. Enumerable.Range(1, outsideSpells).Select(i => $"outside_{i}"), .. Enumerable.Range(1, 4 - outsideSpells).Select(i => $"inside_{i}")];

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, spellIds), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.SchoolNotAllowed");
    }

    [Fact]
    public async Task Handle_WithASpellTheSubclassAddsToTheList_SavesIt()
    {
        // Arrange - a 2014 Fiend patron adds Burning Hands to the warlock's list
        var draft = ArrangeDraft("warlock", level: 1, subclassId: "fiend");
        ArrangeClass("warlock", [Row("warlock", 1, cantrips: 0, spells: 2, maxSpellLevel: 1)], new Subclass
        {
            Id = "fiend",
            Name = "The Fiend",
            Source = "PHB",
            ClassId = "warlock",
            Spells = [new SpellSubclass { SpellId = "burning_hands", SubclassId = "fiend", Level = 1, IsAlwaysPrepared = false }]
        });
        ArrangeSpellList("warlock", Spell("hex", 1));
        _spellRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<List<string>>(ids => ids.SequenceEqual(new[] { "burning_hands" })), _cts.Token))
            .ReturnsAsync([Spell("burning_hands", 1)]);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["hex", "burning_hands"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedSpellIds.Should().Equal("hex", "burning_hands");
    }

    [Fact]
    public async Task Handle_WithADomainSpellTheClassListHasToo_ReturnsAlwaysPrepared()
    {
        // Arrange - Bless is on the cleric list and a Life cleric's domain spell: it is prepared
        // already and does not count, so picking it would waste one of the two picks
        var draft = ArrangePreparedCasterDraft("cleric", "Wisdom", 1, 12, raceBonus: 0, subclassId: "life");
        ArrangePreparedCaster("cleric", "Wisdom", 1, 1, new Subclass
        {
            Id = "life",
            Name = "Life Domain",
            Source = "PHB",
            ClassId = "cleric",
            Spells = [new SpellSubclass { SpellId = "bless", SubclassId = "life", Level = 1, IsAlwaysPrepared = true }]
        });
        ArrangeSpellList("cleric", Spell("bless", 1), Spell("cure_wounds", 1));

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["bless", "cure_wounds"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Spell.AlwaysPrepared");
        result.Errors[0].Description.Should().Be("bless comes with Life Domain, always prepared and not counted; pick another spell in its place.");
    }

    [Fact]
    public async Task Handle_BeforeAClassIsChosen_ReturnsClassRequired()
    {
        // Arrange - previously this looked up a class with a null id and reported Class.NotFound
        ArrangeDraft(classId: null, level: 1);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["fire_bolt"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.ClassRequired");
        _classRepositoryMock.Verify(
            r => r.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeforeALevelIsChosen_ReturnsLevelRequired()
    {
        // Arrange - the spell table is read by level
        var draft = ArrangeDraft("wizard", level: null);
        ArrangeClass("wizard", [Row("wizard", 1, cantrips: 3, spells: 6, maxSpellLevel: 1)]);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["fire_bolt"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.LevelRequired");
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["fire_bolt"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASpellFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - Absorb Elements is from Xanathar's Guide; the draft allows the Player's Handbook alone
        var draft = ArrangeDraft("wizard", level: 1);
        ArrangeClass("wizard", [Row("wizard", 1, cantrips: 0, spells: 1, maxSpellLevel: 1)]);
        var absorbElements = Spell("absorb_elements", 1);
        absorbElements.Source = "XGE";
        ArrangeSpellList("wizard", absorbElements);

        // Act
        var result = await _sut.Handle(new ChooseSpellsCommand(_draftId, ["absorb_elements"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.SourceNotAllowed");
        result.Errors[0].Description.Should().Be("The spell absorb_elements comes from XGE, a book this character does not allow.");
    }

    private void AssertRefused(Response result, CharacterDraft draft, string code)
    {
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(code);
        draft.SelectedSpellIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft ArrangeDraft(string? classId, int? level, string? subclassId = null)
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);

        if (level is { } characterLevel)
        {
            draft.SetNameAndLevel("Elminster", characterLevel);
        }

        if (classId is not null)
        {
            draft.SetClass(classId, subclassId);
        }

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private void ArrangeClass(string classId, List<ClassSpellProgression> spellProgression, params Subclass[] subclasses)
    {
        _classRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(classId, _cts.Token))
            .ReturnsAsync(new Class
            {
                Id = classId,
                Name = classId,
                Edition = RulesEdition.Edition2014,
                Source = "PHB",
                HitDie = "1d8",
                SpellProgression = spellProgression,
                Subclasses = [.. subclasses]
            });
    }

    // A 2014 class whose table gives no spell count at this level and no cantrips, up to level 1
    // spells, preparing its modifier plus its level divided by the divisor
    private void ArrangePreparedCaster(string classId, string ability, int divisor, int level, params Subclass[] subclasses)
    {
        _classRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(classId, _cts.Token))
            .ReturnsAsync(new Class
            {
                Id = classId,
                Name = classId,
                Edition = RulesEdition.Edition2014,
                Source = "PHB",
                HitDie = "1d8",
                Spellcasting = new Spellcasting
                {
                    ClassId = classId, SpellcastingAbility = ability, IsPreparedCaster = true, PreparedSpellsLevelDivisor = divisor
                },
                SpellProgression = [Row(classId, level, cantrips: 0, spells: 0, maxSpellLevel: 1)],
                Subclasses = [.. subclasses]
            });
    }

    // A dwarf soldier ready to finalize but for its spells, its spellcasting ability at this base
    // score, which the dwarf raises by raceBonus
    private CharacterDraft ArrangePreparedCasterDraft(string classId, string ability, int level, int score, int raceBonus, string? subclassId = null)
    {
        var draft = ArrangeDraft(classId, level, subclassId);
        var abilityId = AbilityScore.Normalize(ability);
        var scores = new Dictionary<string, int> { ["STR"] = 10, ["DEX"] = 10, ["CON"] = 10, ["INT"] = 10, ["WIS"] = 10, ["CHA"] = 10 };
        scores[abilityId] = score;
        draft.SetRace("dwarf");
        draft.SetBackground("soldier");
        draft.SetBaseAbilityScores(scores, AbilityScoreMethod.Rolled);

        _raceRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token))
            .ReturnsAsync(new Race
            {
                Id = "dwarf",
                Name = "Dwarf",
                Edition = RulesEdition.Edition2014,
                Source = "PHB",
                Speed = 25,
                Size = "Medium",
                AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = abilityId, Bonus = raceBonus }],
            });
        _backgroundRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token))
            .ReturnsAsync(new Background { Id = "soldier", Name = "Soldier", Edition = RulesEdition.Edition2014, Source = "PHB", FeatureId = "military-rank" });
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([]);
        _languageRepositoryMock.Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync([]);

        return draft;
    }

    // level_1_spell_1 and on, all level 1
    private static SpellReadModel[] LevelOneSpells(int count) =>
        [.. Enumerable.Range(1, count).Select(i => Spell($"level_1_spell_{i}", 1))];

    private void ArrangeSpellList(string classId, params SpellReadModel[] spells)
    {
        _spellRepositoryMock
            .Setup(r => r.GetByClassIdAsync(classId, _cts.Token))
            .ReturnsAsync([.. spells]);
    }

    private static ClassSpellProgression Row(string classId, int level, int cantrips, int spells, int maxSpellLevel) => new()
    {
        ClassId = classId,
        CharacterLevel = level,
        CantripsKnown = cantrips,
        SpellsKnown = spells,
        MaxSpellLevel = maxSpellLevel
    };

    // A subclass with a table at this level of one cantrip and so many level 1 spells, whose
    // spells come from two schools but for one from any school at each of levels 3, 8, 14 and 20
    private static Subclass SchoolLimitedSubclass(string id, string classId, List<string> schools, int level, int spellsKnown) => new()
    {
        Id = id,
        Name = id,
        Source = "PHB",
        ClassId = classId,
        SpellListClassId = "wizard",
        SpellSchools = schools,
        AnySchoolSpellLevels = [3, 8, 14, 20],
        SpellProgression =
        [
            new SubclassSpellProgression
            {
                SubclassId = id, CharacterLevel = level, CantripsKnown = 1, SpellsKnown = spellsKnown, MaxSpellLevel = 1, SpellcastingAbility = "INT"
            }
        ]
    };

    // outside_1.. are necromancy spells, inside_1.. spells of the given school, all level 1
    private static SpellReadModel[] SchoolSpells(int outside, int inside, string school) =>
    [
        .. Enumerable.Range(1, outside).Select(i => Spell($"outside_{i}", 1, "Necromancy")),
        .. Enumerable.Range(1, inside).Select(i => Spell($"inside_{i}", 1, school)),
    ];

    private static SpellReadModel Cantrip(string id) => Spell(id, 0);

    private static SpellReadModel Spell(string id, int level, string school = "Evocation") => new()
    {
        Id = id,
        Name = id,
        Source = "PHB",
        Level = level,
        Description = "A spell.",
        School = school,
        CastingTimeUnit = "action",
        RangeType = "ranged",
        DurationType = "instantaneous"
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
