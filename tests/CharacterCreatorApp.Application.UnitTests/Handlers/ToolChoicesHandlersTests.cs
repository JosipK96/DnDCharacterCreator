using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseTools;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetToolChoices;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

/// <summary>
/// ChooseTools and GetToolChoices for a dwarf (one of three artisan's tools), a Criminal (one
/// gaming set, beside the thieves' tools it gives) and a bard (three instruments).
/// </summary>
public class ToolChoicesHandlersTests : IDisposable
{
    private static readonly List<string> GamingSets = ["dice_set", "dragonchess", "playing_card_set", "three_dragon_ante"];
    private static readonly List<string> Instruments = ["bagpipes", "drum", "flute", "lute", "lyre"];

    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IProficiencyRepository> _proficiencyRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly ChooseToolsCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public ToolChoicesHandlersTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new ChooseToolsCommandHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _classRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithAPickForEachChoiceOfADwarfCriminalBard_SavesThemInOrder()
    {
        // Arrange
        var draft = ArrangeDwarfCriminalBard();

        // Act
        var result = await _sut.Handle(
            new ChooseToolsCommand(_draftId, [["smith_tools"], ["dice_set"], ["lute", "lyre", "drum"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.ToolPicks.Should().BeEquivalentTo(new List<List<string>> { new() { "smith_tools" }, new() { "dice_set" }, new() { "lute", "lyre", "drum" } }, o => o.WithStrictOrdering());
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAChoiceLeftOut_ReturnsToolChoicesIncomplete()
    {
        // Arrange - the bard's instruments are missing
        var draft = ArrangeDwarfCriminalBard();

        // Act
        var result = await _sut.Handle(new ChooseToolsCommand(_draftId, [["smith_tools"], ["dice_set"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Character.ToolChoicesIncomplete");
        result.Errors[0].Description.Should().Contain("Dwarf, Criminal, Bard").And.Contain("Got 2 pick(s)");
        draft.ToolPicks.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithADwarfToolTheRaceDoesNotOffer_ReturnsInvalidToolPick()
    {
        // Arrange - a dwarf picks smith's tools, brewer's supplies or mason's tools
        var draft = ArrangeDwarfCriminalBard();

        // Act
        var result = await _sut.Handle(
            new ChooseToolsCommand(_draftId, [["woodcarver_tools"], ["dice_set"], ["lute", "lyre", "drum"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidToolPick");
        result.Errors[0].Description.Should().StartWith("Dwarf offers 1");
        draft.ToolPicks.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAnInstrumentForTheCriminalsGamingSet_ReturnsInvalidToolPick()
    {
        // Arrange
        var draft = ArrangeDwarfCriminalBard();

        // Act
        var result = await _sut.Handle(
            new ChooseToolsCommand(_draftId, [["smith_tools"], ["flute"], ["lute", "lyre", "drum"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidToolPick");
        result.Errors[0].Description.Should().StartWith("Criminal offers 1");
        draft.ToolPicks.Should().BeEmpty();
    }

    [Theory]
    [InlineData("lute lyre")]
    [InlineData("lute lute lyre")]
    public async Task Handle_WithoutThreeDifferentInstrumentsForTheBard_ReturnsInvalidToolPick(string instruments)
    {
        // Arrange
        var draft = ArrangeDwarfCriminalBard();

        // Act
        var result = await _sut.Handle(
            new ChooseToolsCommand(_draftId, [["smith_tools"], ["dice_set"], [.. instruments.Split(' ')]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidToolPick");
        result.Errors[0].Description.Should().StartWith("Bard offers 3");
        draft.ToolPicks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAToolTwoChoicesBothPick_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - a dwarf's smith's tools and a Guild Artisan's would count once; pick another
        var draft = ArrangeDraft(Dwarf(), GuildArtisan(), Fighter());

        // Act
        var result = await _sut.Handle(new ChooseToolsCommand(_draftId, [["smith_tools"], ["smith_tools"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
        result.Errors[0].Description.Should().Be("smith_tools comes with another tool pick already; pick another in its place.");
        draft.ToolPicks.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAToolTheClassGives_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - an Urban Bounty Hunter may pick thieves' tools, which a rogue has already
        var urbanBountyHunter = Background("urban_bounty_hunter", "Urban Bounty Hunter", new ToolChoice { Count = 2, Options = ["dice_set", "lute", "thieves-tools"] });
        var rogue = Class("rogue", "Rogue");
        rogue.ClassProficiencies = [new ClassProficiency { ClassId = "rogue", ProficiencyId = "thieves-tools" }];
        var draft = ArrangeDraft(Race("elf", "Elf"), urbanBountyHunter, rogue);

        // Act
        var result = await _sut.Handle(new ChooseToolsCommand(_draftId, [["dice_set", "thieves-tools"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
        result.Errors[0].Description.Should().Be("thieves-tools comes with Rogue already; pick another in its place.");
        draft.ToolPicks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNoToolChoicesAndNoPicks_SavesNone()
    {
        // Arrange
        var draft = ArrangeDraft(Race("elf", "Elf"), Background("sage", "Sage"), Fighter());

        // Act
        var result = await _sut.Handle(new ChooseToolsCommand(_draftId, []), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.ToolPicks.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_BeforeAClassIsChosen_ReturnsClassRequired()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetRace("dwarf");
        draft.SetBackground("criminal");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new ChooseToolsCommand(_draftId, [["smith_tools"]]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ClassRequired");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetToolChoices_ForADwarfCriminalBard_ListsTheRacesThenTheBackgroundsThenTheClasses()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetRace("dwarf");
        draft.SetBackground("criminal");
        draft.SetClass("bard");
        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        ArrangeSources(Dwarf(), Criminal(), Bard());
        var optionIds = new List<string> { "brewer_supplies", "mason_tools", "smith_tools" }.Concat(GamingSets).Concat(Instruments).ToList();
        _proficiencyRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<List<string>>(ids => ids.SequenceEqual(optionIds)), _cts.Token))
            .ReturnsAsync(optionIds.ConvertAll(id => new ProficiencyReadModel { Id = id, Name = id.ToUpperInvariant(), Type = "tool" }));
        var sut = new GetToolChoicesQueryHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _classRepositoryMock.Object,
            _proficiencyRepositoryMock.Object,
            _userContextMock.Object);

        // Act
        var result = await sut.Handle(new GetToolChoicesQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(c => (c.OfferedBy, c.Count, c.Options.Count)).Should().Equal(("Dwarf", 1, 3), ("Criminal", 1, 4), ("Bard", 3, 5));
        result.Value[0].Options.Select(o => o.Name).Should().Equal("BREWER_SUPPLIES", "MASON_TOOLS", "SMITH_TOOLS");
    }

    private CharacterDraft ArrangeDwarfCriminalBard() => ArrangeDraft(Dwarf(), Criminal(), Bard());

    private CharacterDraft ArrangeDraft(Race race, Background background, Class @class)
    {
        var draft = NewDraft();
        draft.SetRace(race.Id);
        draft.SetBackground(background.Id);
        draft.SetClass(@class.Id);
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        ArrangeSources(race, background, @class);

        return draft;
    }

    private void ArrangeSources(Race race, Background background, Class @class)
    {
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(race.Id, _cts.Token)).ReturnsAsync(race);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(background.Id, _cts.Token)).ReturnsAsync(background);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(@class.Id, _cts.Token)).ReturnsAsync(@class);
    }

    private CharacterDraft NewDraft()
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);

        return draft;
    }

    private static Race Dwarf()
    {
        var dwarf = Race("dwarf", "Dwarf");
        dwarf.ToolChoices = [new ToolChoice { Count = 1, Options = ["brewer_supplies", "mason_tools", "smith_tools"] }];

        return dwarf;
    }

    private static Background Criminal()
    {
        var criminal = Background("criminal", "Criminal", new ToolChoice { Count = 1, Options = GamingSets });
        criminal.BackgroundProficiencies = [new BackgroundProficiency { BackgroundId = "criminal", ProficiencyId = "thieves-tools" }];

        return criminal;
    }

    private static Background GuildArtisan() =>
        Background("guild_artisan", "Guild Artisan", new ToolChoice { Count = 1, Options = ["mason_tools", "smith_tools", "woodcarver_tools"] });

    private static Class Bard()
    {
        var bard = Class("bard", "Bard");
        bard.ToolChoices = [new ToolChoice { Count = 3, Options = Instruments }];

        return bard;
    }

    private static Class Fighter() => Class("fighter", "Fighter");

    private static Race Race(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 25,
    };

    private static Background Background(string id, string name, params ToolChoice[] toolChoices) => new()
    {
        Id = id,
        Name = name,
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        ToolChoices = [.. toolChoices],
    };

    private static Class Class(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        HitDie = "d8",
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
