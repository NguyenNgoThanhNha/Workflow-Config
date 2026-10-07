using WorkflowConfig.Domain.Common;

namespace WorkflowConfig.Domain.Entities.Workflows;

// Bảng cấu hình (admin chỉnh trên màn quản trị, không có máy trạng thái riêng) → setter public (RULES 4.8 cho phép).

/// <summary>Wf_Workflow — quy trình.</summary>
public class Workflow : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary>Loại nhiệm vụ (GT, KS, BH...).</summary>
    public string? CategoryCode { get; set; }

    /// <summary>Một hoặc nhiều mã công ty, phân tách bằng dấu phẩy.</summary>
    public string? CompanyCode { get; set; }

    /// <summary>Đường dẫn tương đối trong IFileStorage.</summary>
    public string? ImagePath { get; set; }

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Khóa trường tiêu đề khi tạo nhiệm vụ.</summary>
    public bool IsSummaryDisabled { get; set; }

    /// <summary>Mã + tên đã chuẩn hóa (SearchNormalizer) để tìm không dấu.</summary>
    public string SearchText { get; private set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<WorkflowStatus> Statuses { get; set; } = new List<WorkflowStatus>();
    public ICollection<StatusTransition> Transitions { get; set; } = new List<StatusTransition>();
    public ICollection<WorkflowFieldConfig> FieldConfigs { get; set; } = new List<WorkflowFieldConfig>();

    public void RefreshSearchText() => SearchText = SearchNormalizer.Normalize($"{Code} {Name}");
}

/// <summary>Wf_Status — trạng thái của quy trình.</summary>
public class WorkflowStatus : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public Workflow? Workflow { get; set; }

    public required string Code { get; set; }
    public required string Name { get; set; }
    public int OrderIndex { get; set; }

    /// <summary>Danh mục yêu cầu (HasRequest) gắn với trạng thái.</summary>
    public string? Category { get; set; }

    /// <summary>Nhóm xử lý (todo/processing/completed...), quyết định màu mặc định.</summary>
    public required string ProcessCode { get; set; }

    /// <summary>Tọa độ X / Y trên sơ đồ.</summary>
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }

    /// <summary>Tự cập nhật ngày kết thúc = ngày hiện tại khi chuyển vào trạng thái này.</summary>
    public bool AutoUpdateEndDate { get; set; }

    public bool IsPushNotification { get; set; }
    public bool IsSendCreator { get; set; }
    public bool IsSendAssignee { get; set; }
    public bool IsSendMonitor { get; set; }
    public string? NotificationTitle { get; set; }
    public string? NotificationMessage { get; set; }

    /// <summary>Màu chữ — null thì lấy màu của Process.</summary>
    public string? TextColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? CustomColor { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<WorkflowStatusFieldRule> FieldRules { get; set; } = new List<WorkflowStatusFieldRule>();
}

/// <summary>Wf_StatusTransition — bước chuyển trạng thái.</summary>
public class StatusTransition : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public Workflow? Workflow { get; set; }

    public Guid FromStatusId { get; set; }
    public WorkflowStatus? FromStatus { get; set; }
    public Guid ToStatusId { get; set; }
    public WorkflowStatus? ToStatus { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? OrderIndex { get; set; }

    /// <summary>Tên nhánh. Nhiều bước chuyển cùng FromStatus + cùng BranchKey được vẽ qua một nút hình thoi.</summary>
    public string? BranchName { get; set; }

    /// <summary>BranchName viết hoa, bỏ dấu, khoảng trắng → '_'.</summary>
    public string? BranchKey { get; set; }

    public int? BranchPositionX { get; set; }
    public int? BranchPositionY { get; set; }

    /// <summary>Điểm nối mũi tên ra (ô nguồn) / vào (ô đích).</summary>
    public string? SourceAnchor { get; set; }
    public string? TargetAnchor { get; set; }

    public string? Color { get; set; }
    public string? TextColor { get; set; }

    /// <summary>Role đặc biệt được phép bấm bước chuyển.</summary>
    public Guid? PermissionRoleId { get; set; }

    public bool IsCreatorAllowed { get; set; }
    public bool IsAssigneeAllowed { get; set; }
    public bool IsReporterAllowed { get; set; }

    public bool IsCommentShown { get; set; }
    public bool IsCommentRequired { get; set; }
    public bool IsDropdownShown { get; set; }
    public bool IsDropdownRequired { get; set; }
    public string? DropdownValueType { get; set; }

    /// <summary>Tự chuyển khi thỏa <see cref="Conditions"/>.</summary>
    public bool IsAutomatic { get; set; }

    /// <summary>Cách cập nhật người được phân công + role (chế độ Roles) hoặc mã phòng ban / nhân viên.</summary>
    public string? AssigneeUpdateMode { get; set; }
    public Guid? AssigneeRoleId { get; set; }
    public string? AssigneeValue { get; set; }

    public string? ReporterUpdateMode { get; set; }
    public Guid? ReporterRoleId { get; set; }
    public string? ReporterValue { get; set; }

    /// <summary>Ký số: NONE/INITIAL/CERTIFICATE + người ký UNIT/PARTNER (bỏ trống khi không ký).</summary>
    public string? SignatureType { get; set; }
    public string? SignerType { get; set; }

    public ICollection<AutoCondition> Conditions { get; set; } = new List<AutoCondition>();
    public ICollection<TransitionNotification> Notifications { get; set; } = new List<TransitionNotification>();

    /// <summary>Đặt tên nhánh và tính lại BranchKey (rỗng → bỏ nhánh).</summary>
    public void SetBranch(string? branchName)
    {
        BranchName = string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim();
        BranchKey = ToBranchKey(BranchName);
    }

    /// <summary>"Duyệt cấp 1" → "DUYET_CAP_1".</summary>
    public static string? ToBranchKey(string? branchName) =>
        string.IsNullOrWhiteSpace(branchName)
            ? null
            : SearchNormalizer.Normalize(branchName).ToUpperInvariant().Replace(' ', '_');
}

/// <summary>Wf_AutoCondition — điều kiện tự động chuyển.</summary>
public class AutoCondition : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransitionId { get; set; }
    public int OrderIndex { get; set; }

    /// <summary>AND/OR nối với điều kiện trước.</summary>
    public string? Connector { get; set; }

    /// <summary>FIELD | TIME.</summary>
    public string? ConditionType { get; set; }

    /// <summary>Mã field (FIELD) hoặc biểu thức thời gian (TIME).</summary>
    public string? Field { get; set; }

    public string? ComparisonType { get; set; }

    /// <summary>INPUT | API.</summary>
    public string? ValueType { get; set; }

    public string? Value { get; set; }

    /// <summary>Câu điều kiện ghép sẵn: Connector + Field + Comparison + Value.</summary>
    public string? SqlText { get; set; }
}

/// <summary>Wf_TransitionNotification — cấu hình gửi thông báo khi chuyển trạng thái.</summary>
public class TransitionNotification : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransitionId { get; set; }
    public int OrderIndex { get; set; }

    public required string Type { get; set; }
    public string? Mode { get; set; }
    public string? ConfigValue { get; set; }
    public Guid? TemplateId { get; set; }

    // Zalo ZNS
    public string? ZnsTemplateId { get; set; }
    public string? CrmSchema { get; set; }
    public string? CrmTable { get; set; }
    public string? CrmField { get; set; }

    // Push notification về app
    public bool IsSendCreator { get; set; }
    public bool IsSendAssignee { get; set; }
    public bool IsSendMonitor { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }

    public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    public ICollection<NotificationAttachment> Attachments { get; set; } = new List<NotificationAttachment>();
}

/// <summary>Wf_NotificationRecipient — Cc/Bcc của thông báo email.</summary>
public class NotificationRecipient : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationId { get; set; }

    /// <summary>CC | BCC.</summary>
    public required string Kind { get; set; }

    public string? Mode { get; set; }
    public string? ConfigValue { get; set; }
}

/// <summary>Wf_NotificationAttachment — file đính kèm email.</summary>
public class NotificationAttachment : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationId { get; set; }
    public required string Attachment { get; set; }
}

/// <summary>Wf_Field — danh mục trường của nhiệm vụ có thể cấu hình hiển thị.</summary>
public class WorkflowField : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
}

/// <summary>Wf_FieldConfig — trường được chọn hiển thị trong một workflow.</summary>
public class WorkflowFieldConfig : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public required string FieldCode { get; set; }

    public bool IsRequired { get; set; }
    public int? OrderIndex { get; set; }
    public string? Parameters { get; set; }
    public string? Note { get; set; }
    public string? NoteEn { get; set; }
    public bool HideWhenAdd { get; set; }
    public string? AddDefaultValue { get; set; }
    public bool HideWhenEdit { get; set; }
    public string? EditDefaultValue { get; set; }
}

/// <summary>Wf_StatusFieldRule — khóa/bắt buộc field theo trạng thái và vai trò.</summary>
public class WorkflowStatusFieldRule : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public Guid StatusId { get; set; }
    public required string FieldCode { get; set; }

    public bool DisableForCreator { get; set; }
    public bool RequiredForCreator { get; set; }
    public bool DisableForAssignee { get; set; }
    public bool RequiredForAssignee { get; set; }
    public bool DisableForReporter { get; set; }
    public bool RequiredForReporter { get; set; }

    public bool HasAnyFlag =>
        DisableForCreator || RequiredForCreator || DisableForAssignee || RequiredForAssignee || DisableForReporter || RequiredForReporter;
}

/// <summary>Wf_Process — nhóm xử lý của trạng thái và màu mặc định.</summary>
public class WorkflowProcess : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string BackgroundColor { get; set; }
    public required string TextColor { get; set; }
    public int OrderIndex { get; set; }
}

/// <summary>Wf_UpdateMode — cách cập nhật assignee/reporter.</summary>
public class TransitionUpdateMode : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int OrderIndex { get; set; }
}
