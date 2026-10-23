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
        var response = await _sut.Handle(new SetHitPointsCommand(_draftId, result), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeTrue();
        draft.HitDieResults.Should().Equal(new Dictionary<int, int> { [5] = result });
        draft.ChoicesNeeded.Should().NotContain(DraftChoice.HitPoints);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_AResultAboveTheHitDie_ReturnsHitDieResultOutOfRange()
    {
        // Arrange
        var draft = ArrangeLevelUp();

        // Act
        var response = await _sut.Handle(new SetHitPointsCommand(_draftId, 11), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Description.Should().Be("A 1d10 cannot roll 11.");
        draft.HitDieResults.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ADraftThatIsNoLevelUp_ReturnsHitPointsOnlyOnLevelUp()
    {
        // Arrange - a new character's levels take the fixed value
        var draft = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        draft.SetNameAndLevel("Thorin", 5);
        draft.SetClass("fighter");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var response = await _sut.Handle(new SetHitPointsCommand(_draftId, 6), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.HitPointsOnlyOnLevelUp");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SomeoneElsesDraft_ReturnsNotFound()
    {
        // Arrange - the repository finds only the user's own drafts
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var response = await _sut.Handle(new SetHitPointsCommand(_draftId, 6), _cts.Token);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
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
