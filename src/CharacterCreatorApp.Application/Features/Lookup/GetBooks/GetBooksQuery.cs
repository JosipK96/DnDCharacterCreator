using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetBooks;

/// <summary>The books of one rules edition a character may be allowed to use.</summary>
public record GetBooksQuery(string Edition) : IQuery<Response<List<BookReadModel>>>;
