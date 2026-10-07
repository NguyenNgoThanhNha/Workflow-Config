using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Application.Features.V1.Workflows.Services;
using WorkflowConfig.Domain.Entities.Sys;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowLookups;

/// <summary>Danh mục dùng trên các form cấu hình: nhóm xử lý (kèm màu), cách cập nhật assignee/reporter, role, field.</summary>
public sealed record GetWorkflowLookupsQuery : IRequest<WorkflowLookupsDto>;

public sealed class GetWorkflowLookupsQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetWorkflowLookupsQuery, WorkflowLookupsDto>
{
    public async Task<WorkflowLookupsDto> Handle(GetWorkflowLookupsQuery request, CancellationToken ct)
    {
        var processes = await unitOfWork.Repository<WorkflowProcess>().AsNoTracking()
            .OrderBy(p => p.OrderIndex)
            .Select(p => new ProcessDto(p.Code, p.Name, p.BackgroundColor, p.TextColor))
            .ToListAsync(ct);
        var modes = await unitOfWork.Repository<TransitionUpdateMode>().AsNoTracking()
            .OrderBy(m => m.OrderIndex)
            .Select(m => new CodeNameDto(m.Code, m.Name))
            .ToListAsync(ct);
        var roles = await unitOfWork.Repository<SysRole>().AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDto(r.Id, r.Name))
            .ToListAsync(ct);
        var fields = await unitOfWork.Repository<WorkflowField>().AsNoTracking()
            .OrderBy(f => f.OrderIndex)
            .Select(f => new FieldOptionDto(f.Code, f.Name, f.Description))
            .ToListAsync(ct);

        return new WorkflowLookupsDto(processes, modes, roles, fields);
    }
}

/// <summary>Bảng có thể chọn làm nguồn dữ liệu Zalo (GetsTable cũ).</summary>
public sealed record GetCrmTablesQuery : IRequest<IReadOnlyList<string>>;

public sealed class GetCrmTablesQueryHandler(ICrmMetadataReader reader) : IRequestHandler<GetCrmTablesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetCrmTablesQuery request, CancellationToken ct) => reader.GetTablesAsync(ct);
}

/// <summary>Cột nvarchar của một bảng (GetFiledOfTable cũ).</summary>
public sealed record GetCrmColumnsQuery(string Table) : IRequest<IReadOnlyList<string>>;

public sealed class GetCrmColumnsQueryHandler(ICrmMetadataReader reader) : IRequestHandler<GetCrmColumnsQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetCrmColumnsQuery request, CancellationToken ct) => reader.GetColumnsAsync(request.Table, ct);
}
