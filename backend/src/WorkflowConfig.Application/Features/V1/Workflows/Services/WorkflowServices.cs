using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace WorkflowConfig.Application.Features.V1.Workflows.Services;

/// <summary>
/// Đọc metadata bảng/cột của DB (nguồn dữ liệu gửi Zalo). Dùng chung cho query danh mục và SaveTransition (xác định schema).
/// Hệ thống cũ ghép tên bảng vào chuỗi SQL; ở đây luôn dùng SqlParameter (RULES 3.8).
/// </summary>
public interface ICrmMetadataReader
{
    Task<IReadOnlyList<string>> GetTablesAsync(CancellationToken ct);

    /// <summary>Cột kiểu nvarchar của bảng (chỉ trong schema được phép).</summary>
    Task<IReadOnlyList<string>> GetColumnsAsync(string table, CancellationToken ct);

    /// <summary>Schema chứa bảng, null nếu bảng không nằm trong schema được phép.</summary>
    Task<string?> GetSchemaAsync(string table, CancellationToken ct);
}

public sealed class CrmMetadataReader(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, IOptions<WorkflowOptions> options)
    : ICrmMetadataReader
{
    public async Task<IReadOnlyList<string>> GetTablesAsync(CancellationToken ct)
    {
        var (inClause, parameters) = SchemaParameters();
        if (parameters.Count == 0) return [];
        var rows = await unitOfWork.RawSqlQueryAsync<string>(
            $"SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA IN ({inClause}) ORDER BY TABLE_NAME",
            [.. parameters]);
        return rows;
    }

    public async Task<IReadOnlyList<string>> GetColumnsAsync(string table, CancellationToken ct)
    {
        var (inClause, parameters) = SchemaParameters();
        if (parameters.Count == 0) return [];
        parameters.Add(new SqlParameter("@table", table));
        return await unitOfWork.RawSqlQueryAsync<string>(
            $"SELECT COLUMN_NAME AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND DATA_TYPE = 'nvarchar' AND TABLE_SCHEMA IN ({inClause}) ORDER BY ORDINAL_POSITION",
            [.. parameters]);
    }

    public async Task<string?> GetSchemaAsync(string table, CancellationToken ct)
    {
        var (inClause, parameters) = SchemaParameters();
        if (parameters.Count == 0) return null;
        parameters.Add(new SqlParameter("@table", table));
        var rows = await unitOfWork.RawSqlQueryAsync<string>(
            $"SELECT TOP 1 TABLE_SCHEMA AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @table AND TABLE_SCHEMA IN ({inClause})",
            [.. parameters]);
        return rows.FirstOrDefault();
    }

    /// <summary>"@s0, @s1" + tham số tương ứng — danh sách schema lấy từ cấu hình, vẫn truyền bằng tham số.</summary>
    private (string InClause, List<SqlParameter> Parameters) SchemaParameters()
    {
        var schemas = options.Value.NotificationTableSchemas.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        var parameters = schemas.Select((s, i) => new SqlParameter($"@s{i}", s)).ToList();
        return (string.Join(", ", parameters.Select(p => p.ParameterName)), parameters);
    }
}

/// <summary>Đồng bộ danh sách con theo Id: Id có trong input → sửa, Id null/không khớp → thêm, bản ghi cũ không còn → xóa mềm.</summary>
internal static class ChildSync
{
    public static void Apply<TEntity, TInput>(
        DbSet<TEntity> set,
        IReadOnlyCollection<TEntity> existing,
        IReadOnlyList<TInput> inputs,
        Func<TEntity, Guid> entityId,
        Func<TInput, Guid?> inputId,
        Func<TInput, int, TEntity> create,
        Action<TEntity, TInput, int> update)
        where TEntity : class
    {
        var byId = existing.ToDictionary(entityId);
        var kept = new HashSet<Guid>();
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (inputId(input) is { } id && byId.TryGetValue(id, out var entity))
            {
                update(entity, input, i);
                kept.Add(id);
            }
            else
            {
                set.Add(create(input, i));
            }
        }

        set.RemoveRange(existing.Where(e => !kept.Contains(entityId(e))));
    }
}
