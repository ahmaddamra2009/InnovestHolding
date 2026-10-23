namespace InnovestHolding.Models;

/// <summary>
/// A row of an admin list ordered by the admin with ▲▼ (Controllers/Admin/AdminControllerBase.MoveAsync): the list is
/// ordered by <see cref="SortOrder"/> then <see cref="Id"/>, and a move renumbers it 1..n.
/// </summary>
public interface ISortable
{
    int Id { get; }

    int SortOrder { get; set; }
}
