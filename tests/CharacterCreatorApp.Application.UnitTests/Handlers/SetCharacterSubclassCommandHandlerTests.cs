using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetSubclass;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetCharacterSubclassCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetCharacterSubclassCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    public SetCharacterSubclassCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new SetCharacterSubclassCommandHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_ClericAtLevelOne_CanChooseADomain()
    {
        // Arrange - a 2014 cleric chooses their domain at level 1, not level 3
        var draft = ArrangeDraft(level: 1, classId: "cleric", subclassId: "life", subclassLevel: 1);

        // Act
        var result = await _sut.Handle(new SetCharacterSubclassCommand(_draftId, "life"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Description)));
        draft.SubclassId.Should().Be("life");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WizardAtLevelTwo_CanChooseASchool()
    {
        // Arrange - a 2014 wizard chooses at level 2
        var draft = ArrangeDraft(level: 2, classId: "wizard", subclassId: "evocation", subclassLevel: 2);

        // Act
        var result = await _sut.Handle(new SetCharacterSubclassCommand(_draftId, "evocation"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SubclassId.Should().Be("evocation");
    }

    [Fact]
    public async Task Handle_FighterBelowLevelThree_ReturnsValidationError()
    {
        // Arrange - fighters choose at level 3
        ArrangeDraft(level: 2, classId: "fighter", subclassId: "champion", subclassLevel: 3);

        // Act
        var result = await _sut.Handle(new SetCharacterSubclassCommand(_draftId, "champion"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.SubclassLevelRequirement");
        result.Errors[0].Description.Should().Contain("level 3");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASubclassFromAnotherClass_ReturnsValidationError()
    {
        // Arrange
        ArrangeDraft(level: 3, classId: "fighter", subclassId: "champion", subclassLevel: 3);

        // Act
        var result = await _sut.Handle(new SetCharacterSubclassCommand(_draftId, "evocation"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidSubclass");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASubclassFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - Xanathar's Guide is not among the draft's books, the Player's Handbook alone
        var draft = ArrangeDraft(level: 3, classId: "rogue", subclassId: "swashbuckler", subclassLevel: 3, subclassSource: "XGE");

        // Act
        var result = await _sut.Handle(new SetCharacterSubclassCommand(_draftId, "swashbuckler"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The subclass swashbuckler comes from XGE, a book this character does not allow.");
        draft.SubclassId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASubclassFromABookTheDraftAllows_SetsIt()
    {
        // Arrange
        var draft = ArrangeDraft(level: 3, classId: "rogue", subclassId: "swashbuckler", subclassLevel: 3, subclassSource: "XGE");
        draft.AllowBooks(["XGE"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));

        // Act
        var result = await _sut.Handle(new SetCharacterSubclassCommand(_draftId, "swashbuckler"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SubclassId.Should().Be("swashbuckler");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    // The subclass level comes from the class data, which gives each class its own
    private CharacterDraft ArrangeDraft(int level, string classId, string subclassId, int subclassLevel, string subclassSource = "PHB")
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        draft.ChooseEdition(RulesEdition.Edition2014);
        draft.SetNameAndLevel("Thorin", level);
        draft.SetClass(classId);

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        _classRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(classId, _cts.Token))
            .ReturnsAsync(new Class
            {
                Id = classId,
                Name = classId,
                Edition = RulesEdition.Edition2014,
                Source = "PHB",
                HitDie = "1d8",
                SubclassLevel = subclassLevel,
                Subclasses = [new Subclass { Id = subclassId, ClassId = classId, Name = subclassId, Source = subclassSource, Features = [] }]
            });

        return draft;
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
