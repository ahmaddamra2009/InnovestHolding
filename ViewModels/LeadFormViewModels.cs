using InnovestHolding.Infrastructure.Localization;

namespace InnovestHolding.ViewModels;

// Display records of the lead forms (Views/Shared/Blocks/_LeadForm.cshtml), already resolved for the page language.

public sealed record ChoiceVm(int Id, LText Text);

/// <summary>The column beside a form: a heading, a sentence and a list (benefits, or the numbered steps of joining).</summary>
public sealed record FormSideVm(LText Badge, LText Title, LText Text, IReadOnlyList<LText> Points, bool Numbered);

/// <summary>What every lead form shares: where it posts, its ids, the consent sentence, the submit label, the side column.</summary>
public abstract record LeadFormVm(string Action, string IdPrefix, LText Consent, LText SubmitLabel, FormSideVm? Side);

public sealed record ForumRegistrationFormVm(string Action, string IdPrefix, LText Consent, LText SubmitLabel, FormSideVm? Side,
    IReadOnlyList<ChoiceVm> Events, IReadOnlyList<ChoiceVm> Interests)
    : LeadFormVm(Action, IdPrefix, Consent, SubmitLabel, Side);

/// <summary>«Join the Network»: <see cref="Kind"/> is the pre-selected type (from ?type=), null when none.</summary>
public sealed record JoinFormVm(string Action, string IdPrefix, LText Consent, LText SubmitLabel, FormSideVm? Side,
    LText KindQuestion, string? Kind, IReadOnlyList<ChoiceVm> InvestorTypes, IReadOnlyList<ChoiceVm> TicketSizes,
    IReadOnlyList<ChoiceVm> Sectors, IReadOnlyList<ChoiceVm> Stages, IReadOnlyList<ChoiceVm> FundingRanges)
    : LeadFormVm(Action, IdPrefix, Consent, SubmitLabel, Side);

/// <summary>A sponsorship enquiry: the two units (labelled with their page titles) and the packages of each, if any.</summary>
public sealed record SponsorshipFormVm(string Action, string IdPrefix, LText Consent, LText SubmitLabel, FormSideVm? Side,
    string Unit, IReadOnlyList<(string Unit, LText Label, IReadOnlyList<ChoiceVm> Packages)> Units)
    : LeadFormVm(Action, IdPrefix, Consent, SubmitLabel, Side);

/// <summary>
/// «Express interest»: in one of the open <see cref="Opportunities"/> (pre-selected: <see cref="OpportunityId"/>, from the
/// detail page's link) or, with none chosen, in opportunities in general.
/// </summary>
public sealed record InterestFormVm(string Action, string IdPrefix, LText Consent, LText SubmitLabel, FormSideVm? Side,
    IReadOnlyList<ChoiceVm> Opportunities, int? OpportunityId)
    : LeadFormVm(Action, IdPrefix, Consent, SubmitLabel, Side);

/// <summary>
/// A lead form on a page. <see cref="IsContent"/> false: the form sits beside the page's "coming soon" state instead of
/// replacing it (the opportunities page keeps its empty text and its noindex until opportunities are published).
/// </summary>
public sealed record LeadFormBlock(LeadFormVm Form, bool IsContent = true) : BlockVm;
