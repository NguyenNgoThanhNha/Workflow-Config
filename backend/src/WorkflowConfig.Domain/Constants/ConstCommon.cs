namespace WorkflowConfig.Domain.Constants;

public static class ConstMessage
{
    public const string NotFound = "{0} '{1}' không tồn tại.";
    public const string Forbidden = "Bạn không có quyền thực hiện thao tác này.";
    public const string Unauthorized = "Chưa xác thực hoặc thông tin đăng nhập không hợp lệ.";
    public const string InvalidCredentials = "Email hoặc mật khẩu không đúng.";
    public const string EmailExists = "Email đã được sử dụng.";
    public const string ConcurrencyConflict = "Dữ liệu đã được người khác cập nhật. Vui lòng tải lại.";
}

public static class ConstCacheKey
{
    public static string UserPermission(Guid userId, long version) => $"perm:{version}:{userId:N}";
}

public static class ConstPolicy
{
    /// <summary>Tiền tố tên policy động: "PERM:TICKET:C" hoặc "PERM:USER:R|ROLE:R" (OR).</summary>
    public const string PermissionPrefix = "PERM:";
}

public static class ConstTable
{
    public const string Account = "Sys_Account";
    public const string Role = "Sys_Role";
    public const string Activity = "Sys_Activity";
    public const string UserRole = "Sys_UserRole";
    public const string RoleActivity = "Sys_RoleActivity";
    public const string UserActivity = "Sys_UserActivity";
    public const string RefreshToken = "Sys_RefreshToken";
    public const string LogApi = "Sys_LogApi";

    // Cấu hình quy trình
    public const string Workflow = "Wf_Workflow";
    public const string WorkflowStatus = "Wf_Status";
    public const string StatusTransition = "Wf_StatusTransition";
    public const string AutoCondition = "Wf_AutoCondition";
    public const string TransitionNotification = "Wf_TransitionNotification";
    public const string NotificationRecipient = "Wf_NotificationRecipient";
    public const string NotificationAttachment = "Wf_NotificationAttachment";
    public const string WorkflowField = "Wf_Field";
    public const string WorkflowFieldConfig = "Wf_FieldConfig";
    public const string StatusFieldRule = "Wf_StatusFieldRule";
    public const string WorkflowProcess = "Wf_Process";
    public const string TransitionUpdateMode = "Wf_UpdateMode";
}
