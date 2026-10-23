using InnovestHolding.ViewModels;

namespace InnovestHolding.Infrastructure.Site;

/// <summary>
/// The footer's link columns: automatic (today's derivation, filtered by Pages.ShowInFooter and IsPublished) while
/// FooterColumns is empty, else the admin's columns. CONTRACT STUB (C5 Dev 1) — Dev 4 implements it.
/// </summary>
public static class FooterBuilder
{
    public static FooterVm Build(SiteViews v) => FooterVm.Empty;
}
