using CharacterCreatorApp.Application.Common.Behaviours;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

namespace CharacterCreatorApp.Application.UnitTests.Behaviours;

public class ValidationBehaviourTests : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Guid _draftId = new("6f1c2a4e-3b7d-4c9a-8e21-5d0f7b3a9c14");

    [Fact]
    public async Task Handle_WhenARuleFails_ReturnsTheRulesErrorCodeWithThePropertyAsFieldName()
    {
        // Arrange - the real validator, so the code under test is the one the rule declares
        var sut = new ValidationBehaviour<SetCharacterNameAndLevelCommand, Response>(
            new SetCharacterNameAndLevelCommandValidator());
        var handlerCalled = false;

        // Act
        var result = await sut.Handle(
            new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 21, RulesEdition.Edition2014),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(Response.Success());
            },
            _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Type.Should().Be(ErrorType.Validation);
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidLevel");
        result.Errors[0].FieldName.Should().Be("Level");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTheRequestIsValid_CallsTheHandler()
    {
        // Arrange - MediatR 12 hands the handler the caller's token itself, not through the behaviour
        var sut = new ValidationBehaviour<SetCharacterNameAndLevelCommand, Response>(
            new SetCharacterNameAndLevelCommandValidator());
        var handlerCalled = false;

        // Act
        var result = await sut.Handle(
            new SetCharacterNameAndLevelCommand(_draftId, "Thorin", 5, RulesEdition.Edition2014),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(Response.Success());
            },
            _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTheRequestHasNoValidator_CallsTheHandler()
    {
        // Arrange - MediatR resolves the behaviour for every request, validated or not
        var sut = new ValidationBehaviour<SetCharacterNameAndLevelCommand, Response>();
        var handlerCalled = false;

        // Act
        var result = await sut.Handle(
            new SetCharacterNameAndLevelCommand(_draftId, string.Empty, 0, RulesEdition.Edition2014),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(Response.Success());
            },
            _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handlerCalled.Should().BeTrue();
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
