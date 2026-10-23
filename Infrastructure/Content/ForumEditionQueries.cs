using System.Linq.Expressions;
using InnovestHolding.Models;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// F10 contract (Dev 1): the shared definitions of the past editions. <c>EditionsCount</c> of the site snapshot is
/// <c>ListCount(active editions, active editions where <see cref="HasEnglish"/>)</c>; the editions page
/// (<c>PageKeys.ForumEditions</c>, a List page) is thin while the count is 0.
/// </summary>
public static class ForumEditionQueries
{
    /// <summary>An edition whose English page content exists: an English title AND an English text.</summary>
    public static readonly Expression<Func<ForumEdition, bool>> HasEnglish =
        e => e.TitleEn != null && e.TitleEn != "" && e.TextEn != null && e.TextEn != "";

    /// <summary>The public order: SortOrder, then Id (the ISortable rule).</summary>
    public static IQueryable<ForumEdition> Ordered(IQueryable<ForumEdition> editions) =>
        editions.OrderBy(e => e.SortOrder).ThenBy(e => e.Id);
}
