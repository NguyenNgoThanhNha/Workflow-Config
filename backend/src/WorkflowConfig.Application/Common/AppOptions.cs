namespace WorkflowConfig.Application.Common;

/// <summary>Cấu hình chung của ứng dụng (appsettings "App").</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>URL frontend — dùng để dựng link trong email (reset mật khẩu...).</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
