using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Persistence;

public interface ILanguageRepository
{
    /// <summary>
    /// Every language of the edition's book (Language.SourceFor), the ones a class feature
    /// grants included: what a race's, background's or feat's language names resolve against.
    /// </summary>
    Task<List<LanguageReadModel>> GetAllForEditionAsync(string edition, CancellationToken cancellationToken);

    /// <summary>The languages a player may pick for a character of this edition (Language.ChoosableTypes).</summary>
    Task<List<LanguageReadModel>> GetChoosableAsync(string edition, CancellationToken cancellationToken);

    Task<List<LanguageReadModel>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken);
}
