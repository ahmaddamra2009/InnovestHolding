using InnovestHolding.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// Every SaveChanges that adds, changes or deletes a content row drops the site snapshot — controllers cannot
/// forget it. Users and the leads of the public forms (registrations, contact messages, network applications,
/// sponsorship enquiries, opportunity interests) are not page content and do not invalidate.
/// Reviewer rule: ExecuteUpdate / ExecuteDelete / raw SQL bypass interceptors — call ISiteContentCache.Invalidate()
/// next to any such statement on a content table.
/// </summary>
public sealed class ContentCacheInvalidationInterceptor(ISiteContentCache cache) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> NotContent =
    [
        typeof(AppUser), typeof(Registration), typeof(ContactMessage),
        typeof(NetworkApplication), typeof(SponsorshipEnquiry), typeof(OpportunityInterest),
    ];

    // Per-context flag between SavingChanges (entries still visible) and SavedChanges (the write committed).
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<DbContext, object> _pending = new();

    private void Inspect(DbContext? context)
    {
        if (context is null) return;
        var touchesContent = context.ChangeTracker.Entries().Any(e =>
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && !NotContent.Contains(e.Entity.GetType()));
        _pending.Remove(context);
        if (touchesContent) _pending.Add(context, true);
    }

    private void Complete(DbContext? context)
    {
        if (context is not null && _pending.TryGetValue(context, out _))
        {
            _pending.Remove(context);
            cache.Invalidate();
        }
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Inspect(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Inspect(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Complete(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        Complete(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        // A failed save may still have written part of a batch outside a transaction: invalidate to be safe.
        Complete(eventData.Context);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Complete(eventData.Context);
        return Task.CompletedTask;
    }
}
