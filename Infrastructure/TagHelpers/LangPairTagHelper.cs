using InnovestHolding.Infrastructure.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace InnovestHolding.Infrastructure.TagHelpers;

/// <summary>
/// The admin's Arabic/English field pair:
/// <code>&lt;lang-pair label="العنوان" name-ar="TitleAr" name-en="TitleEn" value-ar="…" value-en="…" max="200" rows="3" required="true" /&gt;</code>
/// Arabic first (required when <c>required</c>), English optional with the fallback hint, <c>dir</c>/<c>lang</c> per
/// field, the same maxlength on both, and each field's ModelState error under it. Without <c>name-en</c> it renders
/// ONE language-neutral field (phone, e-mail, URL). Every value is written through TagBuilder (HTML-encoded).
/// </summary>
[HtmlTargetElement("lang-pair", TagStructure = TagStructure.WithoutEndTag)]
public sealed class LangPairTagHelper(IStringLocalizer<SharedResource> l) : TagHelper
{
    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = default!;

    public string Label { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string? NameEn { get; set; }
    public string? ValueAr { get; set; }
    public string? ValueEn { get; set; }
    public int Max { get; set; }
    /// <summary>&gt; 0 → a textarea with that many rows.</summary>
    public int Rows { get; set; }
    public bool Required { get; set; }
    /// <summary>Neutral field written left-to-right (URL, phone, e-mail).</summary>
    public bool Ltr { get; set; }
    public string? Placeholder { get; set; }
    /// <summary>Makes the element ids unique when one page repeats the same field names (inline list forms).</summary>
    public string? IdPrefix { get; set; }
    /// <summary>Resource key of a hint shown under the pair (how the admin should write the value).</summary>
    public string? HintKey { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        var neutral = string.IsNullOrEmpty(NameEn);
        output.Attributes.SetAttribute("class", neutral ? "lang-pair lang-single" : "lang-pair");

        output.Content.AppendHtml(Field(NameAr, ValueAr, neutral ? null : "ar", Required, neutral ? Ltr : false, neutral));
        if (!neutral)
            output.Content.AppendHtml(Field(NameEn!, ValueEn, "en", false, true, false));
        if (!string.IsNullOrEmpty(HintKey))
        {
            var hint = new TagBuilder("p");
            hint.AddCssClass("field-hint");
            hint.AddCssClass("lang-pair-hint");
            hint.InnerHtml.Append(l[HintKey].Value);
            output.Content.AppendHtml(hint);
        }
    }

    private string Id(string name) => TagBuilder.CreateSanitizedId((IdPrefix ?? "") + name, "_") ?? name;

    private IEnumerable<string> Errors(string name) =>
        ViewContext.ViewData.ModelState.TryGetValue(name, out var entry)
            ? entry.Errors.Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m))
            : [];

    private TagBuilder Field(string name, string? value, string? lang, bool required, bool ltr, bool neutral)
    {
        var errors = Errors(name).ToList();
        var group = new TagBuilder("div");
        group.AddCssClass("form-group");
        group.AddCssClass("lang-field");
        group.AddCssClass(lang == "en" ? "lang-en" : "lang-ar");

        var label = new TagBuilder("label");
        label.Attributes["for"] = Id(name);
        label.InnerHtml.Append(Label + " ");
        var chip = new TagBuilder("span");
        chip.AddCssClass("lang-chip");
        if (lang == "en") chip.AddCssClass("en");
        chip.InnerHtml.Append(neutral ? "ع · EN" : lang == "en" ? "EN" : "ع");
        label.InnerHtml.AppendHtml(chip);
        if (required)
        {
            var req = new TagBuilder("span");
            req.AddCssClass("req");
            req.InnerHtml.Append(" *");
            label.InnerHtml.AppendHtml(req);
        }
        group.InnerHtml.AppendHtml(label);

        var input = new TagBuilder(Rows > 0 ? "textarea" : "input");
        input.Attributes["id"] = Id(name);
        input.Attributes["name"] = name;
        input.AddCssClass("form-control");
        if (ltr) input.AddCssClass("ltr-input");
        if (errors.Count > 0) input.AddCssClass("is-invalid");
        input.Attributes["dir"] = ltr ? "ltr" : "rtl";
        if (!neutral) input.Attributes["lang"] = lang;
        if (Max > 0) input.Attributes["maxlength"] = Max.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (required) input.Attributes["required"] = "required";
        if (!string.IsNullOrEmpty(Placeholder) && (neutral || lang == "en")) input.Attributes["placeholder"] = Placeholder;
        if (Rows > 0)
        {
            input.Attributes["rows"] = Rows.ToString(System.Globalization.CultureInfo.InvariantCulture);
            input.InnerHtml.Append(value ?? "");
        }
        else
        {
            input.TagRenderMode = TagRenderMode.SelfClosing;
            input.Attributes["type"] = "text";
            input.Attributes["value"] = value ?? "";
        }
        group.InnerHtml.AppendHtml(input);

        if (lang == "en" || neutral)
        {
            var hint = new TagBuilder("span");
            hint.AddCssClass("field-hint");
            hint.InnerHtml.Append(l[neutral ? MessageKeys.Admin.NeutralHint : MessageKeys.Admin.EnHint].Value);
            group.InnerHtml.AppendHtml(hint);
        }
        foreach (var error in errors)
        {
            var span = new TagBuilder("span");
            span.AddCssClass("field-error");
            span.InnerHtml.Append(error);
            group.InnerHtml.AppendHtml(span);
        }
        return group;
    }
}
