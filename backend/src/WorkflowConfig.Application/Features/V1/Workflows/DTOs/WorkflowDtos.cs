namespace WorkflowConfig.Application.Features.V1.Workflows.DTOs;

// ---------- Danh sách / form workflow ----------

public sealed record WorkflowListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? CategoryCode,
    string? CompanyCode,
    bool HasImage,
    int OrderIndex,
    bool IsActive,
    string? StatusCodes,
    string? CreatedName,
    DateTime CreatedDate);

public sealed record WorkflowStatusRowDto(Guid Id, string Code, string Name, int OrderIndex, string? Category, string ProcessCode);

/// <summary>Một dòng của bảng "Cấu hình thuộc tính": mọi field trong danh mục, IsChosen = đang được dùng trong workflow.</summary>
public sealed record WorkflowFieldConfigDto(
    string FieldCode,
    string FieldName,
    string? Description,
    bool IsChosen,
    bool IsRequired,
    int? OrderIndex,
    string? Parameters,
    string? Note,
    string? NoteEn,
    bool HideWhenAdd,
    string? AddDefaultValue,
    bool HideWhenEdit,
    string? EditDefaultValue);

public sealed record WorkflowDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? CategoryCode,
    string? CompanyCode,
    int OrderIndex,
    bool IsActive,
    bool IsSummaryDisabled,
    bool HasImage,
    byte[] RowVersion,
    string? CreatedName,
    DateTime CreatedDate,
    string? Updater,
    DateTime? UpdatedDate,
    IReadOnlyList<WorkflowStatusRowDto> Statuses,
    IReadOnlyList<WorkflowFieldConfigDto> Fields);

public sealed record WorkflowCopiedDto(Guid Id, string Code, string Name);

// ---------- Danh mục cho form ----------

public sealed record ProcessDto(string Code, string Name, string BackgroundColor, string TextColor);

public sealed record CodeNameDto(string Code, string Name);

public sealed record RoleOptionDto(Guid Id, string Name);

public sealed record FieldOptionDto(string Code, string Name, string? Description);

public sealed record WorkflowLookupsDto(
    IReadOnlyList<ProcessDto> Processes,
    IReadOnlyList<CodeNameDto> UpdateModes,
    IReadOnlyList<RoleOptionDto> Roles,
    IReadOnlyList<FieldOptionDto> Fields);

// ---------- Sơ đồ (màn "Cấu hình workflow") ----------

public sealed record DiagramStatusNodeDto(
    Guid Id,
    string Code,
    string Name,
    string ProcessCode,
    int X,
    int Y,
    string BackgroundColor,
    string TextColor);

/// <summary>Nút hình thoi: Id = "{FromStatusId}+{BranchKey}" (cùng quy ước với hệ thống cũ).</summary>
public sealed record DiagramBranchNodeDto(string Id, Guid FromStatusId, string BranchKey, string Name, int X, int Y);

/// <summary>
/// Mũi tên. Source/Target là id nút (status Guid dạng chuỗi hoặc id nút rẽ nhánh).
/// IsBranchEntry = mũi tên từ trạng thái vào nút rẽ nhánh (không có nhãn).
/// </summary>
public sealed record DiagramEdgeDto(
    string Id,
    Guid TransitionId,
    string Source,
    string Target,
    string? SourceAnchor,
    string? TargetAnchor,
    string? Label,
    string? Color,
    bool IsBranchEntry);

public sealed record WorkflowDiagramDto(
    Guid WorkflowId,
    string Code,
    string Name,
    IReadOnlyList<DiagramStatusNodeDto> Statuses,
    IReadOnlyList<DiagramBranchNodeDto> Branches,
    IReadOnlyList<DiagramEdgeDto> Edges);

/// <summary>Một dòng của bảng cấu hình bước chuyển (màn Config cũ): trạng thái × bước chuyển đi ra.</summary>
public sealed record TransitionTableRowDto(
    Guid StatusId,
    string StatusName,
    string? ProcessName,
    int OrderIndex,
    Guid? TransitionId,
    string? TransitionName,
    string? ToStatusName);

// ---------- Trạng thái ----------

public sealed record StatusFieldRuleDto(
    string FieldCode,
    string? FieldName,
    bool DisableForCreator,
    bool RequiredForCreator,
    bool DisableForAssignee,
    bool RequiredForAssignee,
    bool DisableForReporter,
    bool RequiredForReporter);

/// <summary>Form trạng thái. Id = null khi là form thêm mới (FieldRules vẫn liệt kê đủ field của workflow).</summary>
public sealed record WorkflowStatusFormDto(
    Guid? Id,
    Guid WorkflowId,
    string? Code,
    string? Name,
    int? OrderIndex,
    string? Category,
    string? ProcessCode,
    string TextColor,
    string BackgroundColor,
    string? CustomColor,
    bool AutoUpdateEndDate,
    bool IsPushNotification,
    bool IsSendCreator,
    bool IsSendAssignee,
    bool IsSendMonitor,
    string? NotificationTitle,
    string? NotificationMessage,
    IReadOnlyList<StatusFieldRuleDto> FieldRules);

// ---------- Bước chuyển ----------

public sealed record AutoConditionDto(
    Guid Id,
    string? Connector,
    string? ConditionType,
    string? Field,
    string? ComparisonType,
    string? ValueType,
    string? Value,
    string? SqlText);

public sealed record NotificationRecipientDto(Guid Id, string? Mode, string? ConfigValue);

public sealed record NotificationAttachmentDto(Guid Id, string Attachment);

public sealed record TransitionNotificationDto(
    Guid Id,
    string Type,
    string? Mode,
    string? ConfigValue,
    Guid? TemplateId,
    string? ZnsTemplateId,
    string? CrmSchema,
    string? CrmTable,
    string? CrmField,
    bool IsSendCreator,
    bool IsSendAssignee,
    bool IsSendMonitor,
    string? Title,
    string? Message,
    IReadOnlyList<NotificationRecipientDto> Cc,
    IReadOnlyList<NotificationRecipientDto> Bcc,
    IReadOnlyList<NotificationAttachmentDto> Attachments);

public sealed record TransitionDetailDto(
    Guid Id,
    Guid WorkflowId,
    Guid FromStatusId,
    Guid ToStatusId,
    string Name,
    string? Description,
    int? OrderIndex,
    string? BranchName,
    string? SourceAnchor,
    string? TargetAnchor,
    string? Color,
    string? TextColor,
    Guid? PermissionRoleId,
    bool IsCreatorAllowed,
    bool IsAssigneeAllowed,
    bool IsReporterAllowed,
    bool IsCommentShown,
    bool IsCommentRequired,
    bool IsDropdownShown,
    bool IsDropdownRequired,
    string? DropdownValueType,
    bool IsAutomatic,
    string? AssigneeUpdateMode,
    Guid? AssigneeRoleId,
    string? AssigneeValue,
    string? ReporterUpdateMode,
    Guid? ReporterRoleId,
    string? ReporterValue,
    string? SignatureType,
    string? SignerType,
    IReadOnlyList<AutoConditionDto> Conditions,
    IReadOnlyList<TransitionNotificationDto> Notifications);

public sealed record WorkflowImageDto(Stream Content, string ContentType);
