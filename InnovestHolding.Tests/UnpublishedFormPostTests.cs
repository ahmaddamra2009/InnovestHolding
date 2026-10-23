using System.Net;
using InnovestHolding.Infrastructure.Site;
using Microsoft.EntityFrameworkCore;
using static InnovestHolding.Tests.TestHelpers;

namespace InnovestHolding.Tests;

/// <summary>
/// CTO decision (C5, Dev 1 final): a lead-form POST whose page is unpublished — the page itself or an ancestor —
/// answers 404 like the page, before the body is bound, and writes nothing. Published again, the same POST is accepted.
/// </summary>
[Collection("App")]
public class UnpublishedFormPostTests(TestAppFactory f)
{
    public static readonly TheoryData<string, string, string> Forms = new()
    {
        // unpublished page, form endpoint, page whose form is posted
        { PageKeys.ForumRegistration, "/forms/forum-registration", PageKeys.ForumRegistration },
        { PageKeys.Forum, "/en/forms/forum-registration", PageKeys.ForumRegistration },  // an unpublished hub hides its form
        { PageKeys.NetworkJoin, "/forms/join", PageKeys.NetworkJoin },
        { PageKeys.NetworkJoin, "/en/forms/join", PageKeys.NetworkJoin },
        { PageKeys.TvSponsorship, "/forms/sponsorship", PageKeys.TvSponsorship },
        { PageKeys.NetworkOpportunities, "/forms/opportunity-interest", PageKeys.NetworkOpportunities },
    };

    private Task SetPublishedAsync(string key, bool published) => f.DbAsync(async db =>
    {
        (await db.Pages.SingleAsync(p => p.Key == key)).IsPublished = published;
        return await db.SaveChangesAsync();
    });

    private Task<int> LeadCountAsync(string email) => f.DbAsync(async db =>
        await db.Registrations.CountAsync(r => r.Email == email)
        + await db.NetworkApplications.CountAsync(r => r.Email == email)
        + await db.SponsorshipEnquiries.CountAsync(r => r.Email == email)
        + await db.OpportunityInterests.CountAsync(r => r.Email == email));

    [Theory]
    [MemberData(nameof(Forms))]
    public async Task Post_to_the_form_of_an_unpublished_page_is_404_and_writes_nothing(string unpublish, string endpoint, string formPage)
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync(SiteRoutes.PathFor(SiteRoutes.Get(formPage), endpoint.StartsWith("/en/")));
        var email = Unique("unpub") + "@example.com";
        (string, string)[] body = [("FullName", "QA Unpublished"), ("Email", email), ("Consent", "true")];

        await SetPublishedAsync(unpublish, false);
        try
        {
            var r = await client.PostAsync(endpoint, Form(token, body));
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            Assert.Equal(0, await LeadCountAsync(email));
        }
        finally
        {
            await SetPublishedAsync(unpublish, true);
        }

        // Published: the same POST reaches the action again (accepted, or refused by validation — never 404).
        var again = await client.PostAsync(endpoint, Form(token, body));
        Assert.NotEqual(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact] // the home page's forms are not tied to a sub-page: an unpublished Contact page does not close them
    public async Task Home_forms_ignore_the_publication_of_other_pages()
    {
        var client = f.NewClient();
        var token = await client.GetTokenAsync("/");
        await SetPublishedAsync(PageKeys.Contact, false);
        try
        {
            var r = await client.PostAsync("/forms/contact", Form(token, [("Name", "QA"), ("Email", Unique("c") + "@example.com")]));
            Assert.NotEqual(HttpStatusCode.NotFound, r.StatusCode);
        }
        finally
        {
            await SetPublishedAsync(PageKeys.Contact, true);
        }
    }
}

/// <summary>C5 Dev 1 final: the sections card inside Pages/Edit and the sidebar entry of Admin/PageSections.</summary>
[Collection("App")]
public class PageEditSectionsCardTests(TestAppFactory f)
{
    [Fact]
    public async Task Pages_edit_embeds_the_sections_card_outside_its_form_only_for_pages_with_sections()
    {
        var client = await f.SignedInClientAsync();
        var about = await client.GetStringAsync("/Admin/Pages/Edit/about");
        Assert.Contains("<div class=\"admin-card\" id=\"blocks\">", about);
        Assert.Contains("action=\"/Admin/PageSections/Toggle/about\"", about);
        // The card comes after the page form closes (no nested forms).
        var pageFormEnd = about.IndexOf("</form>", about.IndexOf("معاينة الإنجليزية", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.True(pageFormEnd > 0 && about.IndexOf("id=\"blocks\"", StringComparison.Ordinal) > pageFormEnd);
        Assert.Contains("href=\"/Admin/PageSections\"", about); // sidebar «أقسام الصفحات»

        var privacy = await client.GetStringAsync("/Admin/Pages/Edit/privacy"); // no registry sections
        Assert.DoesNotContain("id=\"blocks\"", privacy);
    }
}
