namespace CharacterCreatorApp.Domain.Common;

/// <summary>
/// Marks an entity whose created and updated timestamps are stamped on save rather than by the
/// code that changes it. AuditableEntityInterceptor fills both from the application's
/// TimeProvider, so nothing has to remember to set them and tests can control the clock.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset? UpdatedAt { get; set; }
}
