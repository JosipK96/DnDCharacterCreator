using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetClass;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterClassCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterClassCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    public SetCharacterClassCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new SetCharacterClassCommandHandler(
            _draftRepositoryMock.Object,
            _userContextMock.Object,
            _classRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithAClassOfTheDraftsEdition_SetsIt()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeClass("wizard-2024", RulesEdition.Edition2024);

        // Act
        var result = await _sut.Handle(new SetCharacterClassCommand(_draftId, "wizard-2024", null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.ClassId.Should().Be("wizard-2024");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAClassOfTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange - a 2014 wizard in a character built with the 2024 rules
        var draft = ArrangeDraft(RulesEdition.Edition2024);
        ArrangeClass("wizard", RulesEdition.Edition2014);

        // Act
        var result = await _sut.Handle(new SetCharacterClassCommand(_draftId, "wizard", null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.EditionMismatch");
        result.Errors[0].Description.Should().Be(
            "The class Wizard follows the 2014 rules, but this character is built with the 2024 rules.");
        draft.ClassId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAClassFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - a class from Tasha's Cauldron, which the draft does not allow
        var draft = ArrangeDraft(RulesEdition.Edition2014);
        ArrangeClass("artificer", RulesEdition.Edition2014, "TCE");

        // Act
        var result = await _sut.Handle(new SetCharacterClassCommand(_draftId, "artificer", null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The class Wizard comes from TCE, a book this character does not allow.");
        draft.ClassId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASubclassFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange
        var draft = ArrangeDraft(RulesEdition.Edition2014);
        var wizard = ArrangeClass("wizard", RulesEdition.Edition2014);
        wizard.Subclasses = [new Subclass { Id = "war-magic", ClassId = "wizard", Name = "War Magic", Source = "XGE", Features = [] }];

        // Act
        var result = await _sut.Handle(new SetCharacterClassCommand(_draftId, "wizard", "war-magic"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The subclass War Magic comes from XGE, a book this character does not allow.");
        draft.ClassId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft ArrangeDraft(string edition)
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        draft.ChooseEdition(edition);
        draft.SetNameAndLevel("Thorin", 1);

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private Class ArrangeClass(string id, string edition, string? source = null)
    {
        var @class = new Class { Id = id, Name = "Wizard", Edition = edition, Source = source ?? RulesEdition.PlayersHandbook(edition), HitDie = "1d6", SubclassLevel = 3 };

        _classRepositoryMock
            .Setup(r => r.GetByIdWithSubclassesAsync(id, _cts.Token))
            .ReturnsAsync(@class);

        return @class;
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
