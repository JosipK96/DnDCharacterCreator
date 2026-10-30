using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;
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

public class ChooseProficienciesCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IProficiencyRepository> _proficiencyRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly ChooseProficienciesCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public ChooseProficienciesCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new ChooseProficienciesCommandHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _proficiencyRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithTwoOfTheClassOptions_SavesThem()
    {
        // Arrange - a fighter picks two of its skills
        var draft = ArrangeHalfElfFighterDraft(raceSkills: ["stealth", "insight"]);
        ArrangeProficiencies("perception", "survival");

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedClassProficiencyIds.Should().Equal("perception", "survival");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithASkillTheRaceSkillPicksHave_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - the half-elf took Athletics already; the fighter picks another of its options
        var draft = ArrangeHalfElfFighterDraft(raceSkills: ["athletics", "stealth"]);
        ArrangeProficiencies("athletics", "perception");

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["athletics", "perception"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOnlyARaceSkillRepeatsTheBackground_SavesTheseOwnPicks()
    {
        // Arrange - the half-elf's Athletics repeats the soldier's; that is for the race skill step
        // or finalize to report, not a reason to refuse a fighter's unrelated picks
        var draft = ArrangeHalfElfFighterDraft(raceSkills: ["athletics", "stealth"]);
        draft.SetBackground("soldier");
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("soldier", _cts.Token)).ReturnsAsync(new Background
        {
            Id = "soldier",
            Name = "Soldier",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            BackgroundProficiencies = [new BackgroundProficiency { BackgroundId = "soldier", ProficiencyId = "athletics" }]
        });
        ArrangeProficiencies("perception", "survival");

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedClassProficiencyIds.Should().Equal("perception", "survival");
    }

    [Fact]
    public async Task Handle_WhenTheDraftIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeforeAClassIsChosen_ReturnsClassRequired()
    {
        // Arrange
        var draft = NewDraft();
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ClassRequired");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheClassIsGone_ReturnsClassNotFound()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetClass("fighter");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync((Class?)null);

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Class.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForAClassWithNoProficiencyChoice_SavesNone()
    {
        // Arrange
        var draft = ArrangeFighterDraft(null);
        draft.SetClassProficiencies(["perception"]);

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTooFewPicks_ReturnsInvalidProficiencyCount()
    {
        // Arrange
        var draft = ArrangeFighterDraft(FighterChoice());

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidProficiencyCount");
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASkillTheClassDoesNotOffer_ReturnsInvalidProficiencySelection()
    {
        // Arrange
        var draft = ArrangeFighterDraft(FighterChoice());

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "arcana"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidProficiencySelection");
        result.Errors[0].Description.Should().StartWith("Invalid proficiency selections: arcana.");
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheSameSkillTwice_ReturnsDuplicateProficiencies()
    {
        // Arrange
        var draft = ArrangeFighterDraft(FighterChoice());

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "perception"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.DuplicateProficiencies");
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASkillMissingFromTheSeedData_ReturnsProficiencyNotFound()
    {
        // Arrange - the class lists it but no proficiency row has it
        var draft = ArrangeFighterDraft(FighterChoice());
        _proficiencyRepositoryMock.Setup(r => r.GetByIdAsync("perception", _cts.Token)).ReturnsAsync((ProficiencyReadModel?)null);

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Proficiency.NotFound");
        draft.SelectedClassProficiencyIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeforeARaceAndABackgroundAreChosen_SavesTheClassPicks()
    {
        // Arrange - nothing else to repeat yet
        var draft = ArrangeFighterDraft(FighterChoice());
        ArrangeProficiencies("perception", "survival");

        // Act
        var result = await _sut.Handle(new ChooseProficienciesCommand(_draftId, ["perception", "survival"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedClassProficiencyIds.Should().Equal("perception", "survival");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    private CharacterDraft NewDraft()
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);

        return draft;
    }

    // A draft with only its class chosen: a fighter that picks two of four skills
    private CharacterDraft ArrangeFighterDraft(ProficiencyChoice? choice)
    {
        var draft = NewDraft();
        draft.SetClass("fighter");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d10",
            ProficiencyChoice = choice,
        });

        return draft;
    }

    private static ProficiencyChoice FighterChoice() => new()
    {
        ClassId = "fighter",
        Type = "skill",
        Count = 2,
        Options = [.. new[] { "athletics", "perception", "survival", "intimidation" }.Select(id => new ProficiencyChoiceOption { ProficiencyId = id })]
    };
    // A level 1 half-elf fighter with these race skills picked, and no background yet
    private CharacterDraft ArrangeHalfElfFighterDraft(List<string> raceSkills)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Tanis", 1);
        draft.SetRace("half_elf");
        draft.SetRaceSkills(raceSkills);
        draft.SetClass("fighter");

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("half_elf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "half_elf",
            Name = "Half-Elf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Speed = 30,
            SkillChoiceCount = 2
        });
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            HitDie = "1d10",
            ProficiencyChoice = new ProficiencyChoice
            {
                ClassId = "fighter",
                Type = "skill",
                Count = 2,
                Options = [.. new[] { "athletics", "perception", "survival", "intimidation" }.Select(id => new ProficiencyChoiceOption { ProficiencyId = id })]
            }
        });

        return draft;
    }

    private void ArrangeProficiencies(params string[] ids)
    {
        foreach (var id in ids)
        {
            _proficiencyRepositoryMock
                .Setup(r => r.GetByIdAsync(id, _cts.Token))
                .ReturnsAsync(new ProficiencyReadModel { Id = id, Name = id, Type = "skill" });
        }
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
