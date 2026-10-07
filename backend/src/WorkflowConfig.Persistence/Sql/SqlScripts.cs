namespace WorkflowConfig.Persistence.Sql;

/// <summary>
/// Đọc script SQL (stored procedure) nhúng trong assembly để migration chạy (chuẩn BE §3.3).
/// Sửa SP: sửa file .sql (luôn dùng CREATE OR ALTER) rồi tạo migration mới gọi lại <see cref="Read"/>.
/// </summary>
public static class SqlScripts
{
    // Khai báo tên file SP ở đây, vd: public const string ProductSearch = "usp_Product_Search.sql";
    // Mẫu SP danh sách (dynamic SQL có tham số): Helpdesk-Ticketing/backend/src/Helpdesk.Persistence/Sql/usp_Ticket_Search.sql

    public static string Read(string fileName)
    {
        var assembly = typeof(SqlScripts).Assembly;
        var resource = assembly.GetManifestResourceNames().SingleOrDefault(n => n.EndsWith("." + fileName, StringComparison.Ordinal))
                       ?? throw new InvalidOperationException($"Không tìm thấy script SQL nhúng '{fileName}'.");
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
