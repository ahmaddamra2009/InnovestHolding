using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class News
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string TitleAr { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TitleEn { get; set; }

    public string SummaryAr { get; set; } = string.Empty;

    public string? SummaryEn { get; set; }

    [MaxLength(50)]
    public string CategoryAr { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CategoryEn { get; set; }

    /// <summary>Free text per language (no date parsing: the admin writes it as it should read).</summary>
    [MaxLength(50)]
    public string NewsDateAr { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NewsDateEn { get; set; }

    /// <summary>"Read more" target; the link is not rendered while empty.</summary>
    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    /// <summary>Public order (ascending). A new item gets MIN − 1 so it shows first.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
