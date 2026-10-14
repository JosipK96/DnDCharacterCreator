using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseRaceSkills;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class ChooseRaceSkillsCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly ChooseRaceSkillsCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public ChooseRaceSkillsCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new ChooseRaceSkillsCommandHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _classRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithTwoSkillsForAHalfElf_SavesThem()
    {
        // Arrange - the seed data types these "skill", which the old check read as not a skill
        var draft = ArrangeDraft(raceId: "half_elf");
        ArrangeRace(HalfElf());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics", "stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedRaceSkillIds.Should().Equal("athletics", "stealth");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFewerSkillsThanTheRaceGives_ReturnsInvalidRaceSkillCount()
    {
        // Arrange
        var draft = ArrangeDraft(raceId: "half_elf");
        ArrangeRace(HalfElf());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidRaceSkillCount");
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheSameSkillTwice_ReturnsDuplicateRaceSkills()
    {
        // Arrange
        var draft = ArrangeDraft(raceId: "half_elf");
        ArrangeRace(HalfElf());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["stealth", "stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.DuplicateRaceSkills");
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAToolInsteadOfASkill_ReturnsInvalidRaceSkillType()
    {
        // Arrange
        var draft = ArrangeDraft(raceId: "half_elf");
        ArrangeRace(HalfElf());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["stealth", "thieves-tools"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidRaceSkillType");
        result.Errors[0].Description.Should().Contain("thieves-tools").And.NotContain("stealth");
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASkillTheChosenBackgroundGives_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - a soldier has Athletics already; the half-elf picks another skill instead
        var draft = ArrangeDraft(raceId: "half_elf");
        draft.SetBackground("soldier");
        ArrangeRace(HalfElf());
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync(new Background
        {
            Id = "soldier",
            Name = "Soldier",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            BackgroundProficiencies = [new BackgroundProficiency { BackgroundId = "soldier", ProficiencyId = "athletics" }]
        });

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics", "stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASkillTheRaceOffers_SavesIt()
    {
        // Arrange
        var draft = ArrangeDraft(raceId: "elf_2024", RulesEdition.Edition2024);
        ArrangeRace(Elf2024());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["perception"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedRaceSkillIds.Should().Equal("perception");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithASkillTheRaceDoesNotOffer_ReturnsRaceSkillsNotOffered()
    {
        // Arrange - Stealth is a skill, but not one of Keen Senses' three
        var draft = ArrangeDraft(raceId: "elf_2024", RulesEdition.Edition2024);
        ArrangeRace(Elf2024());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.RaceSkillsNotOffered");
        result.Errors[0].Description.Should().Be("Elf does not offer stealth; choose from insight, perception, survival.");
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheVariantHumansSkill_SavesIt()
    {
        // Arrange - a human picks no skill, a variant human one of any
        var draft = ArrangeDraft(raceId: "human", subraceId: "variant_human");
        ArrangeRace(Human());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["persuasion"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedRaceSkillIds.Should().Equal("persuasion");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ForAStandardHumanWithASkill_ClearsIt()
    {
        // Arrange - the standard human's subrace brings no skill, unlike the variant
        var draft = ArrangeDraft(raceId: "human", subraceId: "standard_human");
        ArrangeRace(Human());

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["persuasion"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedRaceSkillIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ForARaceWithoutSkillChoices_ClearsTheEarlierChoice()
    {
        // Arrange - a half-elf's skills are left over from before the race changed to dwarf
        var draft = ArrangeDraft(raceId: "dwarf");
        draft.SetRaceSkills(["athletics", "stealth"]);
        ArrangeRace(new Race { Id = "dwarf", Name = "Dwarf", Edition = RulesEdition.Edition2014, Source = "PHB", Size = "Medium", Speed = 25 });

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedRaceSkillIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_BeforeARaceIsChosen_ReturnsRaceRequired()
    {
        // Arrange
        ArrangeDraft(raceId: null);

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics", "stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.RaceRequired");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheDraftsRaceNoLongerExists_ReturnsRaceNotFound()
    {
        // Arrange
        ArrangeDraft(raceId: "half_elf");
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("half_elf", _cts.Token)).ReturnsAsync((Race?)null);

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics", "stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Race.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new ChooseRaceSkillsCommand(_draftId, ["athletics", "stealth"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft ArrangeDraft(string? raceId, string edition = RulesEdition.Edition2014, string? subraceId = null)
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(edition);

        if (raceId is not null)
        {
            draft.SetRace(raceId, subraceId);
        }

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private void ArrangeRace(Race race) =>
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(race.Id, _cts.Token)).ReturnsAsync(race);

    // Skill Versatility: any two skills
    private static Race HalfElf() => new()
    {
        Id = "half_elf",
        Name = "Half-Elf",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        SkillChoiceCount = 2,
        LanguageChoiceCount = 1,
        RaceLanguages = [new RaceLanguage { RaceId = "half_elf", Language = "Common" }, new RaceLanguage { RaceId = "half_elf", Language = "Elvish" }],
    };

    // No skill of its own; the variant human's subrace brings one of any
    private static Race Human() => new()
    {
        Id = "human",
        Name = "Human",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        Subraces =
        [
            new Subrace { Id = "standard_human", Name = "Standard Human", Source = "PHB", RaceId = "human" },
            new Subrace { Id = "variant_human", Name = "Variant Human", Source = "PHB", RaceId = "human", SkillChoiceCount = 1 },
        ],
    };

    // Keen Senses: one of three skills
    private static Race Elf2024() => new()
    {
        Id = "elf_2024",
        Name = "Elf",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        Size = "Medium",
        Speed = 30,
        SkillChoiceCount = 1,
        SkillChoiceOptions = ["insight", "perception", "survival"],
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
