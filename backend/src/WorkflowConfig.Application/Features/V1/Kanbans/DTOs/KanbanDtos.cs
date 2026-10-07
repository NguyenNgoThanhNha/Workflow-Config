namespace WorkflowConfig.Application.Features.V1.Kanbans.DTOs;

public sealed record KanbanListItemDto(
    Guid Id,
    string Code,
    string Name,
    int OrderIndex,
    bool IsActive,
    int ColumnCount,
    int MappedStatusCount,
    string? CreatedName,
    DateTime CreatedDate);

public sealed record KanbanColumnDto(Guid Id, string Name, int OrderIndex, string? Note, string? Color);

public sealed record KanbanDetailDto(
    Guid Id,
    string Code,
    string Name,
    int OrderIndex,
    bool IsActive,
    byte[] RowVersion,
    string? CreatedName,
    DateTime CreatedDate,
    string? Updater,
    DateTime? UpdatedDate,
    IReadOnlyList<KanbanColumnDto> Columns);

/// <summary>Workflow có thể lọc trên bảng (chỉ workflow đang có trạng thái).</summary>
public sealed record KanbanWorkflowOptionDto(Guid Id, string Code, string Name);

/// <summary>Một thẻ trên bảng = một trạng thái workflow. ColumnId = null → cột "Chưa cấu hình".</summary>
public sealed record KanbanCardDto(
    Guid StatusId,
    Guid WorkflowId,
    string WorkflowCode,
    string WorkflowName,
    string StatusCode,
    string StatusName,
    string ProcessCode,
    string BackgroundColor,
    string TextColor,
    Guid? ColumnId);

public sealed record KanbanBoardDto(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyList<KanbanColumnDto> Columns,
    IReadOnlyList<KanbanWorkflowOptionDto> Workflows,
    IReadOnlyList<KanbanCardDto> Cards);
