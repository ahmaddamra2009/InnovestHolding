using InnovestHolding.Infrastructure.Content;
using InnovestHolding.Infrastructure.Forms;
using InnovestHolding.Infrastructure.Localization;
using InnovestHolding.Models;
using InnovestHolding.ViewModels;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>Builds the lead forms' display records for a page language (texts from the database, labels from the resx).</summary>
public static class LeadFormViews
{
    public const string ForumRegistration = "forum-registration";
    public const string Join = "join";
    public const string Sponsorship = "sponsorship";
    public const string OpportunityInterest = "opportunity-interest";

    private static IReadOnlyList<ChoiceVm> Choices(this SiteViews v, IEnumerable<FormChoice> list) =>
        list.Select(c => new ChoiceVm(c.Id, v.P(c.TextAr, c.TextEn))).ToList();

    private static LText Consent(SiteViews v) => v.Content.Text(ContentKeys.FormsConsent);

    public static ForumRegistrationFormVm ForumRegistrationForm(this SiteViews v, IReadOnlyList<FormChoice> events, LText submit) => new(
        CultureUrls.Form(ForumRegistration, v.English), "forum-reg", Consent(v), submit,
        new FormSideVm(v.Content.Text("register_badge"), v.Content.Text("register_title"), v.Content.Text("register_text"),
            v.Snapshot.Benefits.Select(b => v.P(b.Ar, b.En)).ToList(), false),
        v.Choices(events),
        v.Snapshot.Interests.Select(i => new ChoiceVm(i.Id, v.P(i.NameAr, i.NameEn))).ToList());

    /// <param name="kind">The ?type= of the request; anything but investor/startup selects nothing.</param>
    public static JoinFormVm JoinForm(this SiteViews v, LeadFormChoices choices, string? kind, LText submit) => new(
        CultureUrls.Form(Join, v.English), "join", Consent(v), submit,
        new FormSideVm(LText.Empty, v.Content.Text("join_side_title"), v.Content.Text("join_side_text"),
            v.Items(ContentItemGroups.NetworkSteps).Select(i => i.Text).ToList(), true),
        v.Content.Text("join_kind_title"),
        kind is NetworkApplication.Investor or NetworkApplication.Startup ? kind : null,
        v.Choices(choices.InvestorTypes), v.Choices(choices.TicketSizes), v.Choices(choices.Sectors),
        v.Choices(choices.Stages), v.Choices(choices.FundingRanges));

    /// <param name="unit">The pre-selected unit (forum / tv; anything else → tv).</param>
    public static SponsorshipFormVm SponsorshipForm(this SiteViews v, string? unit,
        IReadOnlyList<FormChoice> forumPackages, IReadOnlyList<FormChoice> tvPackages, LText submit) => new(
        CultureUrls.Form(Sponsorship, v.English), "sponsor", Consent(v), submit,
        new FormSideVm(LText.Empty, v.Content.Text("sponsorship_form_title"), v.Content.Text("sponsorship_form_text"), [], false),
        unit == SponsorshipEnquiry.Forum ? SponsorshipEnquiry.Forum : SponsorshipEnquiry.Tv,
        [
            (SponsorshipEnquiry.Tv, v.Title(PageKeys.Tv), v.Choices(tvPackages)),
            (SponsorshipEnquiry.Forum, v.Title(PageKeys.Forum), v.Choices(forumPackages)),
        ]);

    /// <param name="opportunities">The open, published opportunities (<see cref="ILeadReferences.OpenOpportunitiesAsync"/>).</param>
    /// <param name="opportunityId">The one to pre-select; ignored unless it is in <paramref name="opportunities"/>.</param>
    public static InterestFormVm InterestForm(this SiteViews v, IReadOnlyList<FormChoice> opportunities, int? opportunityId, LText submit) => new(
        CultureUrls.Form(OpportunityInterest, v.English), "interest", Consent(v), submit,
        new FormSideVm(LText.Empty, v.Content.Text("interest_form_title"), v.Content.Text("interest_form_text"), [], false),
        v.Choices(opportunities),
        LeadFormChoices.Find(opportunities, opportunityId)?.Id);
}
