using CharacterCreatorApp.Domain.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CharacterCreatorApp.Infrastructure.Interceptors;

/// <summary>
/// Stamps CreatedAt and UpdatedAt on every <see cref="IAuditable"/> being saved, so no handler
/// has to remember to. The time comes from the registered TimeProvider, which tests replace with
/// a fake clock.
/// </summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Create() already set this from the same clock; only fill an unset value so
                    // a caller that deliberately backdated an entity is not overwritten.
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = now;
                    }

                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;

                default:
                    break;
            }
        }
    }
}
