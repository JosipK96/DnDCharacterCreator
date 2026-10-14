using MediatR;

namespace CharacterCreatorApp.Application.Common.Abstractions;

public interface ICommand : IRequest
{
}

public interface ICommand<out TResult> : IRequest<TResult>
{
}