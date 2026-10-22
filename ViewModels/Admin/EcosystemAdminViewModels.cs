namespace InnovestHolding.ViewModels.Admin;

/// <summary>A row of an ecosystem admin list (speakers, guests, episodes, opportunities).</summary>
public sealed record AdminRowVm(int Id, string Title, string Sub, bool HasEnglish, bool IsActive, string Slug);

/// <summary>The admin texts of one ecosystem list screen (the admin is Arabic only).</summary>
public sealed record AdminListLabels(string Controller, string Title, string AddLabel, string SubHeader, string PublicPrefix,
    string ImageLabel, string DeleteConfirm, string ActiveLabel = "منشور", string InactiveLabel = "مخفي");

public sealed class AdminListVm
{
    public required PagedListViewModel<AdminRowVm> Page { get; init; }
    public required AdminListLabels Labels { get; init; }
}
