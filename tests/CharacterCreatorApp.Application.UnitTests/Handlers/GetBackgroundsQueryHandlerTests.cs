using CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetBackgroundsQueryHandlerTests : IDisposable
{
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();

    public GetBackgroundsQueryHandlerTests()
    {
        _languageRepositoryMock
            .Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2024, _cts.Token))
            .ReturnsAsync([new LanguageReadModel { Id = "common-xphb", Name = "Common", Type = "Standard" }]);
    }

    [Fact]
    public async Task Handle_GivesTheLanguagesTheirDisplayNames()
    {
        // Arrange - the 2024 data names Common in lower case; a name no language of the book has stays as it is
        _backgroundRepositoryMock
            .Setup(r => r.GetAllAsync(RulesEdition.Edition2024, _cts.Token))
            .ReturnsAsync([Background(["common", "thieves' cant"])]);
        var sut = new GetBackgroundsQueryHandler(_backgroundRepositoryMock.Object, _languageRepositoryMock.Object);

        // Act
        var result = await sut.Handle(new GetBackgroundsQuery(RulesEdition.Edition2024, null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Single().Languages.Should().Equal("Common", "thieves' cant");
    }

    [Fact]
    public async Task ByIdHandle_GivesTheLanguagesTheirDisplayNames()
    {
        // Arrange
        _backgroundRepositoryMock
            .Setup(r => r.GetByIdAsync("acolyte_2024", _cts.Token))
            .ReturnsAsync(Background(["common"]));
        var sut = new GetBackgroundByIdQueryHandler(_backgroundRepositoryMock.Object, _languageRepositoryMock.Object);

        // Act
        var result = await sut.Handle(new GetBackgroundByIdQuery("acolyte_2024"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Languages.Should().Equal("Common");
    }

    [Fact]
    public async Task ByIdHandle_ForAnUnknownBackground_ReturnsNotFound()
    {
        // Arrange
        _backgroundRepositoryMock
            .Setup(r => r.GetByIdAsync("nope", _cts.Token))
            .ReturnsAsync((BackgroundReadModel?)null);
        var sut = new GetBackgroundByIdQueryHandler(_backgroundRepositoryMock.Object, _languageRepositoryMock.Object);

        // Act
        var result = await sut.Handle(new GetBackgroundByIdQuery("nope"), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Background.NotFound");
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private static BackgroundReadModel Background(List<string> languages) =>
        new() { Id = "acolyte_2024", Name = "Acolyte", Edition = RulesEdition.Edition2024, Languages = languages, LanguageChoiceCount = 2 };
}
