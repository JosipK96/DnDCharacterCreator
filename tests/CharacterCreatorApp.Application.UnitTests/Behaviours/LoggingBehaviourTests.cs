using CharacterCreatorApp.Application.Common.Behaviours;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace CharacterCreatorApp.Application.UnitTests.Behaviours;

public class LoggingBehaviourTests : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly LoggingBehaviour<SetCharacterNameAndLevelCommand, Response> _sut =
        new(NullLogger<LoggingBehaviour<SetCharacterNameAndLevelCommand, Response>>.Instance);

    private readonly SetCharacterNameAndLevelCommand _command =
        new(new Guid("9b2d4f61-0c8e-4a37-b5d9-2e7f1c3a6b80"), "Thorin", 5, RulesEdition.Edition2014);

    [Fact]
    public async Task Handle_WhenTheHandlerFails_ReturnsItsErrorsUnchanged()
    {
        // Arrange - the behaviour sits in front of every request, so it must only observe
        Response failure = Error.NotFound("CharacterDraft.NotFound", "Character draft not found");

        // Act
        var result = await _sut.Handle(_command, () => Task.FromResult(failure), _cts.Token);

        // Assert
        result.Should().Be(failure);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
