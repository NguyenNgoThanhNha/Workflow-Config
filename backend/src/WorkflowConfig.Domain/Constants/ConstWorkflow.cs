namespace WorkflowConfig.Domain.Constants;

/// <summary>
/// Mã nghiệp vụ của module cấu hình quy trình (port từ WorkFlowController MVC của VAS_CRM).
/// Giá trị giữ nguyên như hệ thống cũ để dữ liệu cấu hình chuyển qua lại được.
/// </summary>
public static class ConstWorkflow
{
    /// <summary>Thư mục lưu ảnh đại diện workflow trong IFileStorage.</summary>
    public const string ImageFolder = "workflows";

    /// <summary>Vị trí mặc định của nút rẽ nhánh (hình thoi) khi chưa từng kéo thả — giống hệ thống cũ.</summary>
    public const int DefaultBranchX = 1031;
    public const int DefaultBranchY = 293;

    /// <summary>Ký tự nối FromStatusId và BranchKey thành id nút rẽ nhánh trên sơ đồ.</summary>
    public const char BranchNodeSeparator = '+';

    /// <summary>Nhóm xử lý của trạng thái (Catalog "process" cũ).</summary>
    public static class Process
    {
        public const string Todo = "todo";
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string Unmapped = "unmapped";
    }

    /// <summary>Cách cập nhật người được phân công / người theo dõi khi chuyển trạng thái (ConstStatusTransition cũ).</summary>
    public static class UpdateMode
    {
        public const string NotConfig = "NotConfig";
        public const string CreateUser = "CreateUser";
        public const string SubmitUser = "SubmitUser";
        public const string Roles = "Roles";
        public const string Remove = "Remove";
        public const string Department = "Department";
        public const string Employee = "Employee";
    }

    /// <summary>Vị trí điểm nối mũi tên trên ô trạng thái.</summary>
    public static class Anchor
    {
        public const string Top = "Top";
        public const string Bottom = "Bottom";
        public const string Left = "Left";
        public const string Right = "Right";

        public static readonly IReadOnlyList<string> All = [Top, Bottom, Left, Right];
    }

    /// <summary>Cấu hình ký số của bước chuyển (TransitionSignConfig cũ).</summary>
    public static class Signature
    {
        public const string None = "NONE";
        public const string Initial = "INITIAL";
        public const string Certificate = "CERTIFICATE";

        public const string SignerUnit = "UNIT";
        public const string SignerPartner = "PARTNER";

        public static readonly IReadOnlyList<string> Types = [None, Initial, Certificate];
        public static readonly IReadOnlyList<string> Signers = [SignerUnit, SignerPartner];
    }

    /// <summary>Kênh gửi thông báo khi chuyển trạng thái.</summary>
    public static class NotificationType
    {
        public const string Email = "EMAIL";
        public const string Sms = "SMS";
        public const string Push = "PUSH_NOTIFICATION";
        public const string Zalo = "Zalo";

        public static readonly IReadOnlyList<string> All = [Push, Zalo, Email, Sms];
    }

    /// <summary>Nguồn dữ liệu Zalo mặc định (DataZalo = Default) của hệ thống cũ.</summary>
    public static class ZaloDefault
    {
        public const string Table = "TaskModel";
        public const string Field = "Text7";
    }

    /// <summary>Loại người nhận Cc/Bcc của thông báo email.</summary>
    public static class RecipientKind
    {
        public const string Cc = "CC";
        public const string Bcc = "BCC";
    }

    /// <summary>Điều kiện tự động chuyển trạng thái.</summary>
    public static class Condition
    {
        public const string And = "AND";
        public const string Or = "OR";

        public const string TypeField = "FIELD";
        public const string TypeTime = "TIME";

        public const string ValueInput = "INPUT";
        public const string ValueApi = "API";

        public static readonly IReadOnlyList<string> Connectors = [And, Or];
        public static readonly IReadOnlyList<string> Types = [TypeField, TypeTime];
        public static readonly IReadOnlyList<string> Comparisons = ["=", "<", ">", "<=", ">="];
        public static readonly IReadOnlyList<string> ValueTypes = [ValueInput, ValueApi];
    }
}
