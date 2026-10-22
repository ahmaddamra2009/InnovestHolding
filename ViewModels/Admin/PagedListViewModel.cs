namespace InnovestHolding.ViewModels.Admin;

/// <summary>One page of an admin list that grows (registrations, messages), newest first.</summary>
public sealed class PagedListViewModel<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int UnreadCount { get; init; }

    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>A requested page clamped to 1…(last page reachable from the count).</summary>
    public static int Clamp(int? page, int totalCount, int pageSize)
    {
        var last = Math.Max(1, (totalCount + pageSize - 1) / pageSize);
        return Math.Clamp(page ?? 1, 1, last);
    }
}
