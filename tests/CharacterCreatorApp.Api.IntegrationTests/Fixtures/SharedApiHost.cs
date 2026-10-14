namespace CharacterCreatorApp.Api.IntegrationTests.Fixtures;

/// <summary>
/// Every API test class shares one host and one PostgreSQL container. The host takes its
/// connection string and signing key from process environment variables, which one factory per
/// class would overwrite for each other while running in parallel; one container is also faster
/// than three. Each test class still resets the database after every test.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SharedApiHost : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
