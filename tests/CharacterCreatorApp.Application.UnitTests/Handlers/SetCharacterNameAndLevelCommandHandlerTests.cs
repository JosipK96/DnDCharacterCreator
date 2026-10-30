using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterNameAndLevelCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterNameAndLevelCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public SetCharacterNameAndLevelCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new SetCharacterNameAndLevelCommandHandler(
            _draftRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithANameLevelAndEdition_SetsThemAndSaves()
    {
        // Arrange
        var draft = NewDraft();
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 5, RulesEdition.Edition2014), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.Name.Should().Be("Thorin");
        draft.Level.Should().Be(5);
        draft.Edition.Should().Be(RulesEdition.Edition2014);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAnotherEdition_ClearsTheRaceClassAndBackground()
    {
        // Arrange - the editions share no ids, so a pick from the old one cannot stay
        var draft = NewDraft();
        draft.SetRace("dwarf");
        draft.SetClass("fighter");
        draft.SetBackground("soldier");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 3, RulesEdition.Edition2024), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.Edition.Should().Be(RulesEdition.Edition2024);
        draft.RaceId.Should().BeNull();
        draft.ClassId.Should().BeNull();
        draft.BackgroundId.Should().BeNull();
        draft.Level.Should().Be(3);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTheSameEdition_KeepsTheRaceClassAndBackground()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetRace("dwarf");
        draft.SetClass("fighter");
        draft.SetBackground("soldier");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 3, RulesEdition.Edition2014), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.RaceId.Should().Be("dwarf");
        draft.ClassId.Should().Be("fighter");
        draft.BackgroundId.Should().Be("soldier");
    }

    [Fact]
    public async Task Handle_WithAnotherLevel_ClearsTheChoicesTheLevelDecides()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetNameAndLevel("Thorin", 5);
        draft.SetSpells(["fire_bolt"]);
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 6, RulesEdition.Edition2014), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.Level.Should().Be(6);
        draft.SelectedSpellIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTheDraftIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 1, RulesEdition.Edition2014), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft NewDraft()
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);

        return draft;
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
