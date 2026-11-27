using CharacterCreatorApp.Contracts.Responses.Feats;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Improvements;

public static class FeatChoiceExtensions
{
    /// <summary>
    /// Whether the feat's choice is skills or tools the player picks on this step (Keen Mind's skill,
    /// Skilled's three). Resilient's saving throw is not one: it follows the ability the feat raises.
    /// </summary>
    public static bool AsksForProficiencies(this FeatChoiceApiResponse choice) =>
        choice.Type is FeatChoiceApiResponse.Skills or FeatChoiceApiResponse.SkillsTools;
}
