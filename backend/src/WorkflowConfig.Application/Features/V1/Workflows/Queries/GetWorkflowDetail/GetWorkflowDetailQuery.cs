using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowDetail;

/// <summary>Dữ liệu form sửa workflow: thông tin chung + trạng thái + toàn bộ danh mục field (đánh dấu field đang dùng).</summary>
public sealed record GetWorkflowDetailQuery(Guid Id) : IRequest<WorkflowDetailDto>;

public sealed class GetWorkflowDetailQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetWorkflowDetailQuery, WorkflowDetailDto>
{
    public Task<WorkflowDetailDto> Handle(GetWorkflowDetailQuery request, CancellationToken ct) =>
        WorkflowDetailReader.ReadAsync(unitOfWork, request.Id, ct);
}

/// <summary>Đọc form sửa workflow — dùng chung cho query và SaveWorkflowCommand (trả về sau khi lưu).</summary>
public static class WorkflowDetailReader
{
    public static async Task<WorkflowDetailDto> ReadAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, Guid id, CancellationToken ct)
    {
        var header = await unitOfWork.Repository<Workflow>().AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new
            {
                w.Id, w.Code, w.Name, w.CategoryCode, w.CompanyCode, w.OrderIndex, w.IsActive, w.IsSummaryDisabled,
                HasImage = w.ImagePath != null, w.RowVersion, w.CreatedName, w.CreatedDate, w.Updater, w.UpdatedDate
            })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Workflow", id);

        var statuses = await unitOfWork.Repository<WorkflowStatus>().AsNoTracking()
            .Where(s => s.WorkflowId == id)
            .OrderBy(s => s.OrderIndex)
            .Select(s => new WorkflowStatusRowDto(s.Id, s.Code, s.Name, s.OrderIndex, s.Category, s.ProcessCode))
            .ToListAsync(ct);

        var fields = await FieldConfigReader.ReadAsync(unitOfWork, id, ct);

        return new WorkflowDetailDto(
            header.Id, header.Code, header.Name, header.CategoryCode, header.CompanyCode, header.OrderIndex, header.IsActive,
            header.IsSummaryDisabled, header.HasImage, header.RowVersion, header.CreatedName, header.CreatedDate, header.Updater,
            header.UpdatedDate, statuses, fields);
    }
}

/// <summary>Bảng "Cấu hình thuộc tính" cho form tạo mới: toàn bộ danh mục field, chưa chọn field nào.</summary>
public sealed record GetWorkflowFieldTemplateQuery : IRequest<IReadOnlyList<WorkflowFieldConfigDto>>;

public sealed class GetWorkflowFieldTemplateQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetWorkflowFieldTemplateQuery, IReadOnlyList<WorkflowFieldConfigDto>>
{
    public Task<IReadOnlyList<WorkflowFieldConfigDto>> Handle(GetWorkflowFieldTemplateQuery request, CancellationToken ct) =>
        FieldConfigReader.ReadAsync(unitOfWork, null, ct);
}

/// <summary>Field đang dùng (theo thứ tự cấu hình) rồi tới các field còn lại của danh mục.</summary>
public static class FieldConfigReader
{
    public static async Task<IReadOnlyList<WorkflowFieldConfigDto>> ReadAsync(
        IUnitOfWork<WorkflowConfigDbContext> unitOfWork, Guid? workflowId, CancellationToken ct)
    {
        var catalog = await unitOfWork.Repository<WorkflowField>().AsNoTracking()
            .OrderBy(f => f.OrderIndex)
            .Select(f => new { f.Code, f.Name, f.Description, f.OrderIndex })
            .ToListAsync(ct);

        var configs = workflowId is null
            ? []
            : await unitOfWork.Repository<WorkflowFieldConfig>().AsNoTracking()
                .Where(c => c.WorkflowId == workflowId)
                .ToListAsync(ct);
        var byCode = configs.ToDictionary(c => c.FieldCode, StringComparer.OrdinalIgnoreCase);

        return catalog
            .Select(f => byCode.TryGetValue(f.Code, out var c)
                ? new WorkflowFieldConfigDto(f.Code, f.Name, f.Description, true, c.IsRequired, c.OrderIndex, c.Parameters, c.Note,
                    c.NoteEn, c.HideWhenAdd, c.AddDefaultValue, c.HideWhenEdit, c.EditDefaultValue)
                : new WorkflowFieldConfigDto(f.Code, f.Name, f.Description, false, false, null, null, null, null, false, null, false, null))
            .OrderByDescending(f => f.IsChosen)
            .ThenBy(f => f.IsChosen ? f.OrderIndex ?? int.MaxValue : 0)
            .ThenBy(f => catalog.FindIndex(c => c.Code == f.FieldCode))
            .ToList();
    }
}
