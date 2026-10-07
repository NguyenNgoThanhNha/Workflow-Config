using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.Application.Features.V1.Maintenance.Commands.PurgeExpiredData;

/// <summary>Cấu hình appsettings "DataRetention" — thời hạn giữ dữ liệu kỹ thuật (RULES 4.11).</summary>
public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    /// <summary>Xóa refresh token đã hết hạn quá N ngày (token revoke vẫn giữ tới khi hết hạn để phát hiện reuse).</summary>
    public int RefreshTokenGraceDays { get; set; } = 1;

    /// <summary>Số dòng mỗi lần DELETE — nhỏ hơn ngưỡng lock escalation (~5000) để không khóa cả bảng.</summary>
    public int BatchSize { get; set; } = 4000;
}

public sealed record PurgeExpiredDataResult(int RefreshTokens);

/// <summary>
/// Dọn dữ liệu kỹ thuật tăng mãi (refresh token...). Job nền DataRetentionService gọi mỗi ngày.
/// Ngoại lệ có chủ đích của quy tắc xóa mềm: dữ liệu kỹ thuật hết hạn được XÓA CỨNG bằng ExecuteDeleteAsync
/// (+ IgnoreQueryFilters để dọn cả dòng đã xóa mềm). Bảng kỹ thuật mới (thông báo, OTP...) → thêm vào đây.
/// Mẫu có thêm bảng thông báo: Helpdesk-Ticketing/backend (PurgeExpiredDataCommand).
/// </summary>
public sealed record PurgeExpiredDataCommand : IRequest<PurgeExpiredDataResult>;

public sealed class PurgeExpiredDataCommandHandler(
    IUnitOfWork<WorkflowConfigDbContext> unitOfWork,
    IOptions<DataRetentionOptions> options,
    TimeProvider clock,
    ILogger<PurgeExpiredDataCommandHandler> logger) : IRequestHandler<PurgeExpiredDataCommand, PurgeExpiredDataResult>
{
    public async Task<PurgeExpiredDataResult> Handle(PurgeExpiredDataCommand request, CancellationToken ct)
    {
        var o = options.Value;
        var batch = Math.Max(100, o.BatchSize);
        var tokenCutoff = clock.GetUtcNow().UtcDateTime.AddDays(-Math.Max(0, o.RefreshTokenGraceDays));

        var tokens = await DeleteInBatchesAsync(
            unitOfWork.Repository<SysRefreshToken>().IgnoreQueryFilters().Where(t => t.ExpiresAt < tokenCutoff), batch, ct);

        if (tokens > 0) logger.LogInformation("Data retention: deleted {Tokens} refresh tokens", tokens);
        return new PurgeExpiredDataResult(tokens);
    }

    private static async Task<int> DeleteInBatchesAsync<T>(IQueryable<T> query, int batch, CancellationToken ct) where T : class
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await query.Take(batch).ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batch);
        return total;
    }
}
