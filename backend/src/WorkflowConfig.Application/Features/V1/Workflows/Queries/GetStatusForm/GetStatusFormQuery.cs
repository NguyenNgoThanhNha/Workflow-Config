using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetStatusForm;

/// <summary>
/// Form trạng thái trên sơ đồ. StatusId = null → form thêm mới.
/// Bảng "Cấu hình chỉnh sửa task" liệt kê mọi field đang dùng của workflow kèm cờ Disable/Required theo vai trò.
/// </summary>
public sealed record GetStatusFormQuery(Guid WorkflowId, Guid? StatusId) : IRequest<WorkflowStatusFormDto>;

public sealed class GetStatusFormQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetStatusFormQuery, WorkflowStatusFormDto>
{
    public async Task<WorkflowStatusFormDto> Handle(GetStatusFormQuery request, CancellationToken ct)
    {
        if (!await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Id == request.WorkflowId, ct))
            throw new NotFoundException("Workflow", request.WorkflowId);

        var fields = await (
                from c in unitOfWork.Repository<WorkflowFieldConfig>().AsNoTracking()
                where c.WorkflowId == request.WorkflowId
                join f in unitOfWork.Repository<WorkflowField>().AsNoTracking() on c.FieldCode equals f.Code into fj
                from f in fj.DefaultIfEmpty()
                orderby c.OrderIndex, f.OrderIndex
                select new { c.FieldCode, FieldName = f != null ? f.Name : null })
            .ToListAsync(ct);

        if (request.StatusId is not { } statusId)
        {
            return new WorkflowStatusFormDto(null, request.WorkflowId, null, null, null, null, null, "#000000", "#FFFFFF", null,
                false, false, false, false, false, null, null,
                fields.Select(f => new StatusFieldRuleDto(f.FieldCode, f.FieldName, false, false, false, false, false, false)).ToList());
        }

        var status = await unitOfWork.Repository<WorkflowStatus>().AsNoTracking()
            .Where(s => s.Id == statusId && s.WorkflowId == request.WorkflowId)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Trạng thái", statusId);

        var process = await unitOfWork.Repository<WorkflowProcess>().AsNoTracking()
            .Where(p => p.Code == status.ProcessCode)
            .Select(p => new { p.BackgroundColor, p.TextColor })
            .FirstOrDefaultAsync(ct);

        var rules = await unitOfWork.Repository<WorkflowStatusFieldRule>().AsNoTracking()
            .Where(r => r.StatusId == statusId)
            .ToDictionaryAsync(r => r.FieldCode, StringComparer.OrdinalIgnoreCase, ct);

        var fieldRules = fields.Select(f => rules.TryGetValue(f.FieldCode, out var r)
                ? new StatusFieldRuleDto(f.FieldCode, f.FieldName, r.DisableForCreator, r.RequiredForCreator, r.DisableForAssignee,
                    r.RequiredForAssignee, r.DisableForReporter, r.RequiredForReporter)
                : new StatusFieldRuleDto(f.FieldCode, f.FieldName, false, false, false, false, false, false))
            .ToList();

        // Chưa đặt màu riêng → hiện màu mặc định của nhóm xử lý.
        return new WorkflowStatusFormDto(status.Id, status.WorkflowId, status.Code, status.Name, status.OrderIndex, status.Category,
            status.ProcessCode,
            string.IsNullOrEmpty(status.TextColor) ? process?.TextColor ?? "#000000" : status.TextColor,
            string.IsNullOrEmpty(status.BackgroundColor) ? process?.BackgroundColor ?? "#FFFFFF" : status.BackgroundColor,
            status.CustomColor, status.AutoUpdateEndDate, status.IsPushNotification, status.IsSendCreator, status.IsSendAssignee,
            status.IsSendMonitor, status.NotificationTitle, status.NotificationMessage, fieldRules);
    }
}
