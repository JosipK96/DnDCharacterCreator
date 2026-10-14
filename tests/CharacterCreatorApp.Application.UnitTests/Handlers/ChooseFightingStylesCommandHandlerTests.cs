using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseFightingStyles;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class ChooseFightingStylesCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly ChooseFightingStylesCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public ChooseFightingStylesCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new ChooseFightingStylesCommandHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _featRepositoryMock.Object,
            _userContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Theory]
    // The class's own feature: the fighter's at 1, the paladin's and ranger's at 2
    [InlineData("fighter", RulesEdition.Edition2014, null, 1, 1)]
    [InlineData("paladin", RulesEdition.Edition2014, null, 1, 0)]
    [InlineData("paladin", RulesEdition.Edition2014, null, 2, 1)]
    [InlineData("ranger", RulesEdition.Edition2014, null, 2, 1)]
    [InlineData("fighter-2024", RulesEdition.Edition2024, null, 1, 1)]
    [InlineData("paladin-2024", RulesEdition.Edition2024, null, 2, 1)]
    [InlineData("ranger-2024", RulesEdition.Edition2024, null, 2, 1)]
    // A subclass's: College of Swords at 3, and the Champion's second at 10 in 2014 and 7 in 2024
    [InlineData("bard", RulesEdition.Edition2014, "swords", 3, 1)]
    [InlineData("fighter", RulesEdition.Edition2014, "champion", 9, 1)]
    [InlineData("fighter", RulesEdition.Edition2014, "champion", 10, 2)]
    [InlineData("fighter-2024", RulesEdition.Edition2024, "champion-2024", 7, 2)]
    public async Task Handle_WithAsManyStylesAsTheClassOwes_SavesThem(string classId, string edition, string? subclassId, int level, int owed)
    {
        // Arrange
        var draft = ArrangeDraft(classId, edition, level, subclassId);
        ArrangeClass(ClassFor(classId, edition));
        var styles = Enumerable.Range(1, owed).Select(i => Style($"style_{i}", edition, classId)).ToList();
        ArrangeFeats(styles.Select(s => s.Id), styles);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, styles.ConvertAll(s => s.Id)), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Description)));
        draft.FightingStyleIds.Should().Equal(styles.Select(s => s.Id));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoStyleForAFighter_ReturnsFightingStyleCount()
    {
        // Arrange
        var draft = ArrangeDraft("fighter", RulesEdition.Edition2014, level: 1);
        ArrangeClass(ClassFor("fighter", RulesEdition.Edition2014));
        ArrangeFeats([], []);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, []), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.FightingStyleCount");
    }

    [Fact]
    public async Task Handle_WithAFeatThatIsNotAStyle_ReturnsNotAFightingStyle()
    {
        // Arrange
        var draft = ArrangeDraft("fighter", RulesEdition.Edition2014, level: 1);
        ArrangeClass(ClassFor("fighter", RulesEdition.Edition2014));
        var alert = new FeatReadModel { Id = "alert_2014", Name = "Alert", Description = "A feat.", Edition = RulesEdition.Edition2014 };
        ArrangeFeats(["alert_2014"], [alert]);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["alert_2014"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.NotAFightingStyle");
    }

    [Fact]
    public async Task Handle_WithAStyleFromTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange
        var draft = ArrangeDraft("fighter", RulesEdition.Edition2014, level: 1);
        ArrangeClass(ClassFor("fighter", RulesEdition.Edition2014));
        var archery2024 = Style("archery_2024", RulesEdition.Edition2024, "fighter");
        ArrangeFeats(["archery_2024"], [archery2024]);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2024"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.EditionMismatch");
    }

    [Fact]
    public async Task Handle_WithAStyleTheClassDoesNotOffer_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - the 2014 Archery is the fighter's and the ranger's, not the paladin's
        var draft = ArrangeDraft("paladin", RulesEdition.Edition2014, level: 2);
        ArrangeClass(ClassFor("paladin", RulesEdition.Edition2014));
        var archery = Style("archery_2014", RulesEdition.Edition2014, "fighter", "ranger");
        ArrangeFeats(["archery_2014"], [archery]);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2014"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.FeatPrerequisiteNotMet");
    }

    [Fact]
    public async Task Handle_WithTheSameStyleTwiceForAChampion_ReturnsDuplicateFeats()
    {
        // Arrange
        var draft = ArrangeDraft("fighter", RulesEdition.Edition2014, level: 10, subclassId: "champion");
        ArrangeClass(ClassFor("fighter", RulesEdition.Edition2014));
        var archery = Style("archery_2014", RulesEdition.Edition2014, "fighter");
        ArrangeFeats(["archery_2014"], [archery]);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2014", "archery_2014"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.DuplicateFeats");
    }

    [Fact]
    public async Task Handle_WithAStyleAlsoTakenForAnImprovement_ReturnsDuplicateFeats()
    {
        // Arrange - a 2024 style may be an improvement's feat, but not the same one twice
        var draft = ArrangeDraft("fighter-2024", RulesEdition.Edition2024, level: 4);
        draft.SetFeats(["archery_2024"]);
        ArrangeClass(ClassFor("fighter-2024", RulesEdition.Edition2024));
        var archery = Style("archery_2024", RulesEdition.Edition2024);
        ArrangeFeats(["archery_2024"], [archery]);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2024"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.DuplicateFeats");
    }

    [Fact]
    public async Task Handle_WithAFeatTheReferenceDataDoesNotHave_ReturnsInvalidFeatIds()
    {
        // Arrange
        var draft = ArrangeDraft("fighter", RulesEdition.Edition2014, level: 1);
        ArrangeClass(ClassFor("fighter", RulesEdition.Edition2014));
        ArrangeFeats(["no_such_style"], []);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["no_such_style"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.InvalidFeatIds");
    }

    [Fact]
    public async Task Handle_BeforeAClassIsChosen_ReturnsClassRequired()
    {
        // Arrange
        var draft = ArrangeDraft(classId: null, RulesEdition.Edition2014, level: 1);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2014"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "CharacterDraft.ClassRequired");
    }

    [Fact]
    public async Task Handle_WhenTheDraftsClassNoLongerExists_ReturnsClassNotFound()
    {
        // Arrange
        var draft = ArrangeDraft("fighter", RulesEdition.Edition2014, level: 1);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token)).ReturnsAsync((Class?)null);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2014"]), _cts.Token);

        // Assert
        AssertRefused(result, draft, "Class.NotFound");
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new ChooseFightingStylesCommand(_draftId, ["archery_2014"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void AssertRefused(Domain.Common.Results.Response result, CharacterDraft draft, string code)
    {
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        draft.FightingStyleIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft ArrangeDraft(string? classId, string edition, int level, string? subclassId = null)
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(edition);
        draft.SetNameAndLevel("Tester", level);

        if (classId is not null)
        {
            draft.SetClass(classId, subclassId);
        }

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private void ArrangeClass(Class @class) =>
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(@class.Id, _cts.Token)).ReturnsAsync(@class);

    // The lookup is pinned to the picked ids, then the improvement feats
    private void ArrangeFeats(IEnumerable<string> ids, List<FeatReadModel> feats)
    {
        var expected = ids.Distinct().ToList();
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(x => x.SequenceEqual(expected)), _cts.Token))
            .ReturnsAsync(feats);
    }

    // A style needing the Fighting Style feature; a 2014 one is also limited to the classes that list it
    private static FeatReadModel Style(string id, string edition, params string[] classIds) => new()
    {
        Id = id,
        Name = id,
        Description = "A Fighting Style.",
        Edition = edition,
        Category = FeatCategory.FightingStyle,
        Prerequisites = new FeatPrerequisites
        {
            ClassIds = edition == RulesEdition.Edition2014 ? [.. classIds] : [],
            Features = ["Fighting Style"]
        }
    };

    // The classes with the levels of their Fighting Style features, as the seed data has them
    private static Class ClassFor(string classId, string edition)
    {
        int? styleLevel = classId switch
        {
            "fighter" or "fighter-2024" => 1,
            "bard" => null,
            _ => 2,
        };
        List<Subclass> subclasses = classId switch
        {
            "fighter" => [SubclassWithStyle("champion", classId, 10, "Additional Fighting Style")],
            "fighter-2024" => [SubclassWithStyle("champion-2024", classId, 7, "Additional Fighting Style")],
            "bard" => [SubclassWithStyle("swords", classId, 3, "Fighting Style")],
            _ => [],
        };

        return new Class
        {
            Id = classId,
            Name = classId,
            Edition = edition,
            HitDie = "1d10",
            SubclassLevel = 3,
            AsiLevels = [4, 8, 12, 16, 19],
            Features = styleLevel is { } level
                ? [new Feature { Id = $"{classId}-{level}-fighting-style", Name = "Fighting Style", Description = string.Empty, Level = level, ClassId = classId, Type = "class" }]
                : [],
            Subclasses = subclasses
        };
    }

    private static Subclass SubclassWithStyle(string id, string classId, int level, string featureName) => new()
    {
        Id = id,
        Name = id,
        ClassId = classId,
        Features = [new Feature { Id = $"{id}-{level}-style", Name = featureName, Description = string.Empty, Level = level, SubclassId = id, Type = "subclass" }]
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
