using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>
/// Admin › «النسخ السابقة»: one past edition of the forum (F10). Lengths are the columns of <see cref="ForumEdition"/>;
/// the year is checked against <see cref="ForumEdition.MinYear"/>–<see cref="ForumEdition.MaxYear"/>. Never an entity.
/// </summary>
public class ForumEditionInput
{
    [Display(Name = K.AdminForumEditions.Year)]
    [Required(ErrorMessage = K.Required)]
    [Range(ForumEdition.MinYear, ForumEdition.MaxYear, ErrorMessage = K.Range)]
    public int? Year { get; set; }

    [Display(Name = F.Label)]
    [StringLength(60, ErrorMessage = K.MaxLength)]
    public string? EditionLabelAr { get; set; }

    [Display(Name = F.LabelEn)]
    [StringLength(60, ErrorMessage = K.MaxLength)]
    public string? EditionLabelEn { get; set; }

    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Location)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? PlaceAr { get; set; }

    [Display(Name = F.LocationEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? PlaceEn { get; set; }

    [Display(Name = F.Text)]
    [StringLength(4000, ErrorMessage = K.MaxLength)]
    public string? TextAr { get; set; }

    [Display(Name = F.TextEn)]
    [StringLength(4000, ErrorMessage = K.MaxLength)]
    public string? TextEn { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>The edit form: the edition's id and, display-only, its photos.</summary>
public sealed class ForumEditionEditInput : ForumEditionInput
{
    public int Id { get; set; }

    /// <summary>The photos in their order (the first is the cover). Never read from the request.</summary>
    [BindNever]
    public IReadOnlyList<ForumEditionPhotoVm> Photos { get; set; } = [];
}

/// <summary>One photo row of the edit page.</summary>
public sealed record ForumEditionPhotoVm(int Id, MediaRef? Image, string AltAr, string? AltEn);

/// <summary>A row of the editions list.</summary>
public sealed record ForumEditionRowVm(int Id, short Year, string LabelAr, string TitleAr, bool HasEnglish, bool IsActive,
    int PhotoCount, MediaRef? Cover);

/// <summary>Uploads ONE photo to an edition (the shared image pipeline, ImageUse.General).</summary>
public sealed class ForumEditionPhotoInput : ImageFieldsInput
{
    public int EditionId { get; set; }
}

/// <summary>The alt texts of one photo; the photo must belong to <see cref="EditionId"/>.</summary>
public sealed class ForumEditionPhotoAltInput
{
    public int EditionId { get; set; }

    public int PhotoId { get; set; }

    [Display(Name = F.ImageAlt)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? ImageAltAr { get; set; }

    [Display(Name = F.ImageAltEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? ImageAltEn { get; set; }
}

/// <summary>Admin › Partners › groups: a new group, with its «يظهر في شريط المنتدى» switch (F10).</summary>
public class PartnerGroupCreateInput : SimpleListItemInput
{
    /// <summary>The group is listed in the patronage strip of /forum (PartnerGroup.ShowOnForumHub).</summary>
    public bool ShowOnForumHub { get; set; }
}

/// <summary>Admin › Partners › groups: an existing group (name, order, visibility, forum strip).</summary>
public sealed class PartnerGroupUpdateInput : InterestOptionUpdateInput
{
    public bool ShowOnForumHub { get; set; }
}
