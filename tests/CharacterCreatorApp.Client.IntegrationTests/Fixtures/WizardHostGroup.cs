namespace CharacterCreatorApp.Client.IntegrationTests.Fixtures;

/// <summary>
/// One <see cref="WizardHost"/> for every test class here: it sets process-wide environment
/// variables, so two hosts running at once would overwrite each other's database settings.
/// </summary>
[CollectionDefinition(Name)]
public sealed class WizardHostGroup : ICollectionFixture<WizardHost>
{
    public const string Name = "Wizard host";
}
