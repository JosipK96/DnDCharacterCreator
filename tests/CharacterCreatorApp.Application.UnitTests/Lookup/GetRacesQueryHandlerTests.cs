using CharacterCreatorApp.Application.Features.Lookup.GetRaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Lookup;

public class GetRacesQueryHandlerTests : IDisposable
{
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetRacesQueryHandler _sut;

    public GetRacesQueryHandlerTests()
    {
        // The Player's Handbook elf with Mordenkainen's sea elf, and Volo's aasimar
        _raceRepositoryMock.Setup(r => r.GetAllAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(() =>
        [
            Race("aasimar", "VGM"),
            Race("elf", "PHB", Subrace("high_elf", "PHB"), Subrace("sea_elf", "MTF")),
        ]);

        _sut = new GetRacesQueryHandler(_raceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithSources_KeepsOnlyTheRacesAndSubracesFromThoseBooks()
    {
        // Act
        var result = await _sut.Handle(new GetRacesQuery(RulesEdition.Edition2014, ["PHB"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Id.Should().Be("elf");
        result.Value[0].Subraces.Select(s => s.Id).Should().Equal("high_elf");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WithoutSources_KeepsEveryRaceAndSubrace(bool emptySources)
    {
        // Act - a request without ?sources= binds to an empty list
        var result = await _sut.Handle(new GetRacesQuery(RulesEdition.Edition2014, emptySources ? [] : null), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(r => r.Id).Should().Equal("aasimar", "elf");
        result.Value[1].Subraces.Select(s => s.Id).Should().Equal("high_elf", "sea_elf");
    }

    private static RaceReadModel Race(string id, string source, params SubraceReadModel[] subraces) => new()
    {
        Id = id,
        Name = id,
        Edition = RulesEdition.Edition2014,
        Source = source,
        Size = "Medium",
        Subraces = [.. subraces],
    };

    private static SubraceReadModel Subrace(string id, string source) => new() { Id = id, Name = id, RaceId = "elf", Source = source };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
