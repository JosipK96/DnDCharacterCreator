using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;

using FluentAssertions;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetCharacterDetailsQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetCharacterDetailsQueryHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _characterId = Guid.NewGuid();

    public GetCharacterDetailsQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        _sut = new GetCharacterDetailsQueryHandler(
            _characterRepositoryMock.Object,
            _userContextMock.Object,
            new Mock<IProficiencyRepository>(MockBehavior.Strict).Object,
            new Mock<IFeatureRepository>(MockBehavior.Strict).Object,
            new Mock<ISpellRepository>(MockBehavior.Strict).Object,
            new Mock<ILanguageRepository>(MockBehavior.Strict).Object,
            new Mock<IFeatRepository>(MockBehavior.Strict).Object,
            _classRepositoryMock.Object,
            new Mock<IRaceRepository>(MockBehavior.Strict).Object,
            new Mock<IBackgroundRepository>(MockBehavior.Strict).Object);
    }

    [Fact]
    public async Task Handle_WhenTheCharacterIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _characterRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_characterId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDetailsReadModel?)null);

        // Act
        var result = await _sut.Handle(new GetCharacterDetailsQuery(_characterId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.NotFound");
    }

    [Fact]
    public async Task Handle_WhenItsClassIsGone_ReturnsTheClassNotFoundError()
    {
        // Arrange
        _characterRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_characterId, _userId, _cts.Token))
            .ReturnsAsync(new CharacterDetailsReadModel { Id = _characterId, ClassId = "fighter" });
        _classRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync("fighter", _cts.Token))
            .ReturnsAsync((CharacterCreatorApp.Domain.DomainModels.Classes.Class?)null);

        // Act
        var result = await _sut.Handle(new GetCharacterDetailsQuery(_characterId), _cts.Token);

        // Assert
        result.Errors[0].Code.Should().Be("Character.ClassNotFound");
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
