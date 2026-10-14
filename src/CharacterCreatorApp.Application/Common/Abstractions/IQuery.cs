using MediatR;

namespace CharacterCreatorApp.Application.Common.Abstractions;

public interface IQuery<TResponse> : IRequest<TResponse>
{
}