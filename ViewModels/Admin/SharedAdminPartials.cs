namespace InnovestHolding.ViewModels.Admin;

/// <summary>The outcome of AdminControllerBase.MoveAsync: the moved row, its 1-based position, the list size.</summary>
public sealed record MoveResult<T>(T Item, int Position, int Count, bool Moved);

/// <summary>
/// Views/Admin/Shared/_ReorderCell: the ▲▼ pair of one row (two POST forms to <see cref="Action"/> with
/// <c>id</c> + <c>direction</c> + <see cref="Extra"/> hidden fields). ▲ is disabled on the first row, ▼ on the last.
/// </summary>
/// <param name="Name">The row's visible name (aria-label «تحريك «X» للأعلى»).</param>
/// <param name="Position">1-based.</param>
public sealed record ReorderCellVm(string Controller, string Id, string Name, int Position, int Count,
    string Action = "Move", IReadOnlyDictionary<string, string>? Extra = null)
{
    public bool IsFirst => Position <= 1;
    public bool IsLast => Position >= Count;
}

/// <summary>
/// Views/Admin/Shared/_VisibleSwitch: the «ظاهر / مخفي» button of one row, posting <see cref="Action"/> with <c>id</c>
/// (+ <see cref="Extra"/>). <see cref="Disabled"/> renders it inert (e.g. a section that cannot be hidden).
/// <see cref="TitleKey"/>: a resx key ({0} = the name) for the tooltip when a row has two switches (QA C5-6); null = the
/// default «تبديل ظهور «X»».
/// </summary>
public sealed record VisibleSwitchVm(string Controller, string Id, string Name, bool Visible,
    string Action = "Toggle", IReadOnlyDictionary<string, string>? Extra = null, bool Disabled = false, string? TitleKey = null);

/// <summary>
/// Views/Admin/Shared/_IconPicker: a radio grid over Infrastructure/Content/Icons.All bound to <see cref="Field"/>.
/// <see cref="AllowNone"/> adds «بدون أيقونة» (empty value).
/// </summary>
public sealed record IconPickerVm(string Field, string? Selected, string Legend = "الأيقونة", bool AllowNone = true, string? NoneLabel = null);
