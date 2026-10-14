using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetLanguages;

/// <summary>The languages a character of this rules edition may pick; the 2014 ones when Edition is null.</summary>
public record GetLanguagesQuery(string? Edition = null) : IQuery<Response<List<LanguageReadModel>>>;
