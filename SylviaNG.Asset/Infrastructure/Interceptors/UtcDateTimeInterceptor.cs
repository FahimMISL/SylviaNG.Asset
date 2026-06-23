using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SylviaNG.Assets.SharedKernel.Utils;

namespace SylviaNG.Assets.Infrastructure.Interceptors;

public class UtcDateTimeInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureUtc(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureUtc(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void EnsureUtc(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            foreach (var property in entry.Properties)
            {
                if (property is { IsModified: false, EntityEntry.State: EntityState.Unchanged }) continue;

                if (property.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                {
                    property.CurrentValue = DateTimeUtility.ConvertLocalToUtc(dt);
                }

                if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableDt && nullableDt.Kind != DateTimeKind.Utc)
                {
                    property.CurrentValue = DateTimeUtility.ConvertLocalToUtc(nullableDt);
                }
            }
        }
    }
}
