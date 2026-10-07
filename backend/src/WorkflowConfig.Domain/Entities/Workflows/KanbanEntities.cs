using WorkflowConfig.Domain.Common;

namespace WorkflowConfig.Domain.Entities.Workflows;

/// <summary>Wf_Kanban — bảng Kanban: gom trạng thái của nhiều workflow vào các cột chung.</summary>
public class Kanban : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Mã + tên đã chuẩn hóa (SearchNormalizer) để tìm không dấu.</summary>
    public string SearchText { get; private set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<KanbanColumn> Columns { get; set; } = new List<KanbanColumn>();

    public void RefreshSearchText() => SearchText = SearchNormalizer.Normalize($"{Code} {Name}");
}

/// <summary>Wf_KanbanColumn — một cột của bảng Kanban.</summary>
public class KanbanColumn : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KanbanId { get; set; }
    public required string Name { get; set; }
    public int OrderIndex { get; set; }
    public string? Note { get; set; }
    public string? Color { get; set; }
}

/// <summary>
/// Wf_KanbanStatusMapping — trạng thái workflow nằm ở cột nào của bảng Kanban.
/// Mỗi trạng thái thuộc tối đa một cột trong một bảng; không có dòng nào = "Chưa cấu hình".
/// </summary>
public class KanbanStatusMapping : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KanbanId { get; set; }
    public Guid ColumnId { get; set; }
    public Guid StatusId { get; set; }
}
