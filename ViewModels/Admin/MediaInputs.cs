using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure.Content;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

/// <summary>
/// The image part of an admin form (Views/Admin/Shared/_ImageField): an optional new file, a "remove" switch and the
/// alt text in both languages. The file itself is validated by Infrastructure/Media/MediaStore, never bound to an
/// entity; <see cref="Current"/> is display-only.
/// </summary>
public abstract class ImageFieldsInput
{
    public IFormFile? Image { get; set; }

    public bool RemoveImage { get; set; }

    [Display(Name = F.ImageAlt)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? ImageAltAr { get; set; }

    [Display(Name = F.ImageAltEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? ImageAltEn { get; set; }

    /// <summary>The stored image, shown as a preview. Never read from the request.</summary>
    [BindNever]
    public MediaRef? Current { get; set; }

    /// <summary>A file was chosen (an empty file input posts a part with no name and no bytes).</summary>
    public bool HasUpload => Image is { } f && (f.Length > 0 || !string.IsNullOrEmpty(f.FileName));
}

/// <summary>The founder block's photo (Content editor).</summary>
public sealed class FounderPhotoInput : ImageFieldsInput;
