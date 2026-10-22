using System.ComponentModel.DataAnnotations;
using InnovestHolding.Infrastructure;
using F = InnovestHolding.Infrastructure.Localization.MessageKeys.Fields;
using K = InnovestHolding.Infrastructure.Localization.MessageKeys;

namespace InnovestHolding.ViewModels.Admin;

public class EventInput : ImageFieldsInput, IValidatableObject
{
    [Display(Name = F.Slug)]
    [StringLength(Infrastructure.Site.Slugs.MaxLength, ErrorMessage = K.MaxLength)]
    public string? Slug { get; set; }

    [Display(Name = F.Summary)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryAr { get; set; }

    [Display(Name = F.SummaryEn)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    public string? SummaryEn { get; set; }

    /// <summary>Local date and time where the event takes place (combined with <see cref="UtcOffset"/>).</summary>
    [Display(Name = F.StartsAt)]
    public DateTime? StartsAtLocal { get; set; }

    [Display(Name = F.EndsAt)]
    public DateTime? EndsAtLocal { get; set; }

    [Display(Name = F.UtcOffset)]
    [StringLength(6, ErrorMessage = K.MaxLength)]
    public string? UtcOffset { get; set; }

    public Models.EventStatus Status { get; set; }

    public Models.AttendanceMode AttendanceMode { get; set; }

    [Display(Name = F.City)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? CityAr { get; set; }

    [Display(Name = F.CityEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? CityEn { get; set; }

    [Display(Name = F.Address)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? AddressAr { get; set; }

    [Display(Name = F.AddressEn)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? AddressEn { get; set; }

    [Display(Name = F.OnlineUrl)]
    [StringLength(500, ErrorMessage = K.MaxLength)]
    [HttpsUrl]
    public string? OnlineUrl { get; set; }

    public bool RegistrationOpen { get; set; } = true;

    [Display(Name = F.MetaTitle)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? MetaTitleAr { get; set; }

    [Display(Name = F.MetaTitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? MetaTitleEn { get; set; }

    [Display(Name = F.MetaDescription)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? MetaDescriptionAr { get; set; }

    [Display(Name = F.MetaDescriptionEn)]
    [StringLength(300, ErrorMessage = K.MaxLength)]
    public string? MetaDescriptionEn { get; set; }

    /// <summary>The start with its offset, or null when no date was entered (or the offset is invalid).</summary>
    public DateTimeOffset? StartsAt() => Combine(StartsAtLocal);

    public DateTimeOffset? EndsAt() => Combine(EndsAtLocal);

    private DateTimeOffset? Combine(DateTime? local) =>
        local is { } l && Infrastructure.Site.EventDates.TryParseOffset(UtcOffset, out var offset)
            ? new DateTimeOffset(DateTime.SpecifyKind(l, DateTimeKind.Unspecified), offset)
            : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var l = context.GetService(typeof(Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>))
            as Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>;
        string M(string key) => l?[key].Value ?? key;
        if (StartsAtLocal is null && (EventDay is null || string.IsNullOrWhiteSpace(EventMonthAr)))
            yield return new ValidationResult(M(K.DateRequired), [nameof(StartsAtLocal)]);
        if ((StartsAtLocal is not null || EndsAtLocal is not null) && !Infrastructure.Site.EventDates.TryParseOffset(UtcOffset, out _))
            yield return new ValidationResult(M(K.UtcOffset), [nameof(UtcOffset)]);
        if (EndsAtLocal is not null && StartsAtLocal is null)
            yield return new ValidationResult(M(K.DateRequired), [nameof(StartsAtLocal)]);
        if (EndsAtLocal is { } end && StartsAtLocal is { } start && end <= start)
            yield return new ValidationResult(M(K.EndBeforeStart), [nameof(EndsAtLocal)]);
        if (!Enum.IsDefined(Status) || !Enum.IsDefined(AttendanceMode))
            yield return new ValidationResult(M(K.InvalidValueNoField), [nameof(Status)]);
    }

    [Display(Name = F.Title)]
    [Required(ErrorMessage = K.Required)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleAr { get; set; }

    [Display(Name = F.TitleEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? TitleEn { get; set; }

    [Display(Name = F.Description)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? DescriptionAr { get; set; }

    [Display(Name = F.DescriptionEn)]
    [StringLength(InputLimits.LongTextMax, ErrorMessage = K.MaxLength)]
    public string? DescriptionEn { get; set; }

    /// <summary>Free-text day/month: required only when no start date is entered (then shown as is).</summary>
    [Display(Name = F.EventDay)]
    [Range(1, 31, ErrorMessage = K.Range)]
    public int? EventDay { get; set; }

    [Display(Name = F.EventMonth)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? EventMonthAr { get; set; }

    [Display(Name = F.EventMonthEn)]
    [StringLength(50, ErrorMessage = K.MaxLength)]
    public string? EventMonthEn { get; set; }

    [Display(Name = F.Location)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? LocationAr { get; set; }

    [Display(Name = F.LocationEn)]
    [StringLength(200, ErrorMessage = K.MaxLength)]
    public string? LocationEn { get; set; }

    [Display(Name = F.Time)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TimeAr { get; set; }

    [Display(Name = F.TimeEn)]
    [StringLength(100, ErrorMessage = K.MaxLength)]
    public string? TimeEn { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EventEditInput : EventInput
{
    public int Id { get; set; }

    [Display(Name = F.SortOrder)]
    [Range(0, 100_000, ErrorMessage = K.Range)]
    public int SortOrder { get; set; }
}
