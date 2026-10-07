namespace WorkflowConfig.Application.Features.V1.Workflows.Services;

/// <summary>Cấu hình module quy trình (appsettings "Workflow").</summary>
public sealed class WorkflowOptions
{
    public const string SectionName = "Workflow";

    /// <summary>
    /// Schema được phép chọn bảng/cột làm nguồn dữ liệu Zalo (hệ thống cũ: 'Task', 'Customer').
    /// Chỉ liệt kê BASE TABLE trong các schema này — không lộ toàn bộ DB.
    /// </summary>
    public string[] NotificationTableSchemas { get; set; } = ["dbo"];

    /// <summary>Dung lượng tối đa ảnh đại diện workflow.</summary>
    public long ImageMaxBytes { get; set; } = 2 * 1024 * 1024;

    public string[] ImageExtensions { get; set; } = [".png", ".jpg", ".jpeg", ".gif", ".webp"];
}
