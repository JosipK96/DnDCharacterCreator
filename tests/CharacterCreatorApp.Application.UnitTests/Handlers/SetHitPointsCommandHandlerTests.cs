using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetHitPointsCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 23, 9, 0, 0, TimeSpan.Zero));
    private readonly CancellationTokenSource _cts = new();
    private readonly SetHitPointsCommandHandler _sut;

    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _draftId = Guid.CreateVersion7();

    public SetHitPointsCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);
        _classRepositoryMock.Setup(r => r.GetByIdAsync("fighter", _cts.Token)).ReturnsAsync(new ClassReadModel
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            HitDie = "1d10",
        });

        _sut = new SetHitPointsCommandHandler(
            _draftRepositoryMock.Object, _classRepositoryMock.Object, _unitOfWorkMock.Object, _userContextMock.Object);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task Handle_AResultTheHitDieRolls_KeepsItForTheNewLevel(int result)
    {
        // Arrange - a d10: from 1 to 10, the fixed 6 among them
        var draft = ArrangeLevelUp();

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [5] = result }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeTrue();
        draft.HitDieResults.Should().Equal(new Dictionary<int, int> { [5] = result });
        draft.ChoicesNeeded.Should().NotContain(DraftChoice.HitPoints);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ANewCharacterAboveLevelOne_KeepsTheResultsItPickedAndLeavesTheRestToTheFixedValue()
    {
        // Arrange - a level 5 fighter rolled level 3 once; now it rolls 2 and 5 and leaves 3 and 4 alone
        var draft = ArrangeNewCharacter(level: 5);
        draft.SetHitDieResults(new Dictionary<int, int> { [3] = 2 });

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [2] = 9, [5] = 1 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeTrue();
        draft.HitDieResults.Should().Equal(new Dictionary<int, int> { [2] = 9, [5] = 1 });
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_AResultAboveTheHitDie_ReturnsHitDieResultOutOfRange()
    {
        // Arrange
        var draft = ArrangeLevelUp();

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [5] = 11 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        var error = response.Errors.Should().ContainSingle().Which;
        error.Code.Should().Be("CharacterDraft.HitDieResultOutOfRange");
        error.Description.Should().Be("A 1d10 cannot roll 11.");
        draft.HitDieResults.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(6, 5)]
    [InlineData(2, 1)]
    public async Task Handle_ANewCharactersLevelItDoesNotRollFor_ReturnsHitDieLevelNotRolled(int level, int characterLevel)
    {
        // Arrange - level 1 is the die's maximum, and a level above the character's is not reached yet
        var draft = ArrangeNewCharacter(characterLevel);

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [level] = 4 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.HitDieLevelNotRolled");
        draft.HitDieResults.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ALevelUpForAnEarlierLevel_ReturnsHitDieLevelNotRolled()
    {
        // Arrange - the earlier levels kept what they were given when they were reached
        var draft = ArrangeLevelUp();

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [4] = 3, [5] = 6 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.HitDieLevelNotRolled");
        draft.HitDieResults.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ALevelUpWithoutItsLevel_ReturnsHitDieResultRequired()
    {
        // Arrange
        var draft = ArrangeLevelUp();

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int>()), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.HitDieResultRequired");
        draft.ChoicesNeeded.Should().Contain(DraftChoice.HitPoints);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ADraftWithNoLevel_ReturnsLevelRequired()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.SetClass("fighter");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [2] = 4 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.LevelRequired");
    }

    [Fact]
    public async Task Handle_ADraftWithNoClass_ReturnsClassRequired()
    {
        // Arrange - the hit die is the class's
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.SetNameAndLevel("Thorin", 5);
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [2] = 4 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.ClassRequired");
    }

    [Fact]
    public async Task Handle_AClassTheDataNoLongerHas_ReturnsClassNotFound()
    {
        // Arrange
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.SetNameAndLevel("Thorin", 5);
        draft.SetClass("gunslinger");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _classRepositoryMock.Setup(r => r.GetByIdAsync("gunslinger", _cts.Token)).ReturnsAsync((ClassReadModel?)null);

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [2] = 4 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("Class.NotFound");
    }

    [Fact]
    public async Task Handle_SomeoneElsesDraft_ReturnsNotFound()
    {
        // Arrange - the repository finds only the user's own drafts
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var response = await _sut.Handle(Command(new Dictionary<int, int> { [5] = 6 }), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private SetHitPointsCommand Command(Dictionary<int, int> results) => new(_draftId, results);

    private CharacterDraft ArrangeNewCharacter(int level)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.SetNameAndLevel("Thorin", level);
        draft.SetClass("fighter");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        return draft;
    }

    // A level 4 champion's build opened one level up
    private CharacterDraft ArrangeLevelUp()
    {
        var build = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        build.ChooseEdition(RulesEdition.Edition2014);
        build.SetNameAndLevel("Thorin", 4);
        build.SetClass("fighter", "champion");
        build.MarkAsCompleted(Guid.CreateVersion7());

        var draft = CharacterDraft.StartEdit(build, _timeProvider);
        var champion = new Subclass { Id = "champion", Name = "Champion", ClassId = "fighter" };
        draft.LevelUp(
            new Class { Id = "fighter", Name = "Fighter", Edition = RulesEdition.Edition2014, HitDie = "1d10", SubclassLevel = 3, Subclasses = [champion] },
            champion,
            new Dictionary<string, int>());

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        return draft;
    }
}
