using System.Linq.Expressions;
using InnovestHolding.Models;

namespace InnovestHolding.Infrastructure.Content;

/// <summary>
/// F10: the shared definitions of the past editions. ONE rule decides that an edition's English content is complete
/// (<see cref="IsEnglishComplete"/>): the admin's EN chip, the snapshot's <c>EditionsCount.English</c> (which makes
/// /en/forum/editions indexable, listed in the sitemap and hreflang-paired) and any SQL query (<see cref="HasEnglish"/>)
/// all use it, so they cannot disagree (Hassan F10 🟡1/🔵2).
/// </summary>
public static class ForumEditionQueries
{
    /// <summary>An English title, and English text wherever there is Arabic text (an edition with no text in either
    /// language is complete with its title alone).</summary>
    public static bool IsEnglishComplete(string? titleEn, string? textAr, string? textEn) =>
        !string.IsNullOrWhiteSpace(titleEn) && (string.IsNullOrWhiteSpace(textAr) || !string.IsNullOrWhiteSpace(textEn));

    /// <summary><see cref="IsEnglishComplete"/> as a query expression (same rule; a test pins the two together).</summary>
    public static readonly Expression<Func<ForumEdition, bool>> HasEnglish =
        e => !string.IsNullOrWhiteSpace(e.TitleEn) && (string.IsNullOrWhiteSpace(e.TextAr) || !string.IsNullOrWhiteSpace(e.TextEn));

    /// <summary>The public order: SortOrder, then Id (the ISortable rule).</summary>
    public static IQueryable<ForumEdition> Ordered(IQueryable<ForumEdition> editions) =>
        editions.OrderBy(e => e.SortOrder).ThenBy(e => e.Id);
}
