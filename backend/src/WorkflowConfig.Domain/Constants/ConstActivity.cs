namespace WorkflowConfig.Domain.Constants;

/// <summary>
/// Mã Activity (chức năng) cho phân quyền 6 bảng. Seeder đồng bộ danh sách <see cref="All"/> vào Sys_Activity
/// mỗi lần khởi động (IsSystem = true). Chức năng có code kiểm tra quyền = thêm hằng số + một dòng trong <see cref="All"/>.
/// Ngoài ra admin có thể tự thêm chức năng trên màn Cài đặt → Chức năng (IsSystem = false) — xem RULES 5.2.
/// </summary>
public static class ConstActivity
{
    public const string ApplicationName = "WorkflowConfig";

    // --- Hệ thống (có sẵn trong template) ---
    public const string User = "USER";
    public const string Role = "ROLE";
    public const string ApiLog = "API_LOG";

    // --- Nghiệp vụ ---
    public const string Workflow = "WORKFLOW";
    public const string Kanban = "KANBAN";

    /// <summary>Mặc định một chức năng dùng đủ 4 quyền Thêm/Xem/Sửa/Xóa.</summary>
    public const string AllActions = "CRUD";

    public sealed record Definition(string Code, string Name, string Description, string Actions = AllActions);

    public static readonly IReadOnlyList<Definition> All =
    [
        new(User, "Người dùng", "R: xem user · U: khóa/mở, gán role, cấp quyền riêng", "RU"),
        new(Role, "Vai trò", "C: tạo · R: xem · U: sửa quyền role · D: xóa role"),
        new(ApiLog, "Log API", "R: xem log request/response API để debug", "R"),
        new(Workflow, "Cấu hình quy trình", "C: tạo/copy workflow · R: xem · U: sửa workflow, trạng thái, bước chuyển · D: xóa trạng thái/bước chuyển"),
        new(Kanban, "Bảng Kanban", "C: tạo · R: xem · U: sửa cột, xếp trạng thái vào cột · D: xóa")
    ];
}
