using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation;

/// <summary>
/// The background's feat as the server works it out, for the background and improvements steps.
/// It mirrors Background.PickableFeatCategory and Background.FeatIdWith, which the client cannot
/// reference; keep the three in step.
/// </summary>
public static class BackgroundFeats
{
    /// <summary>
    /// The category a feat picked for the background must be from: a Mist Wanderer's Dark Gift, or
    /// the one a Haunted One may take in place of Survivor. Null when no feat may be picked.
    /// </summary>
    public static string? PickableCategory(BackgroundApiResponse background) =>
        background.FeatChoiceCategory ?? background.FeatAlternativeCategory;

    /// <summary>The feat the character gets: the one picked, where the background allows a pick, else its own.</summary>
    public static string? FeatIdWith(BackgroundApiResponse background, string? pickedFeatId) =>
        PickableCategory(background) == null ? background.Feat?.Id : pickedFeatId ?? background.Feat?.Id;
}
