using System.Text;

using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;

public class GetCharacterSheetPdfQueryHandler(
    ICharacterRepository characterRepository,
    IUserContext userContext,
    IProficiencyRepository proficiencyRepository,
    IFeatureRepository featureRepository,
    ISpellRepository spellRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    ICharacterSheetPdfRenderer renderer) : IQueryHandler<GetCharacterSheetPdfQuery, Response<CharacterSheetPdf>>
{
    private readonly IUserContext _userContext = userContext;
    private readonly ICharacterSheetPdfRenderer _renderer = renderer;

    // The same loader as the sheet page, so the PDF shows the numbers the page does
    private readonly CharacterDetailsLoader _loader = new(
        characterRepository, proficiencyRepository, featureRepository, spellRepository, languageRepository,
        featRepository, classRepository, raceRepository, backgroundRepository);

    public async Task<Response<CharacterSheetPdf>> Handle(GetCharacterSheetPdfQuery query, CancellationToken cancellationToken)
    {
        var response = await _loader.LoadAsync(query.CharacterId, _userContext.UserId, cancellationToken);

        if (!response.IsSuccess)
        {
            return response.Errors;
        }

        var character = response.Value;

        return new CharacterSheetPdf(FileNameFor(character.Name), _renderer.Render(character, query.Paper));
    }

    /// <summary>The name with anything a file system or a Content-Disposition header might refuse taken out.</summary>
    private static string FileNameFor(string name)
    {
        var safe = new StringBuilder();

        foreach (var c in name.Trim())
        {
            safe.Append(char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '_');
        }

        return safe.Length == 0 ? "character.pdf" : $"{safe}.pdf";
    }
}
