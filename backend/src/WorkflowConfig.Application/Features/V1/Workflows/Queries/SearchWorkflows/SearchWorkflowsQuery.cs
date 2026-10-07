using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Common;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.SearchWorkflows;

/// <summary>Màn danh sách: tìm theo mã/tên (không dấu) + trạng thái sử dụng; sắp theo thứ tự hiển thị.</summary>
public sealed record SearchWorkflowsQuery : PagedQuery, IRequest<PagedResult<WorkflowListItemDto>>
{
    public string? Keyword { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class SearchWorkflowsQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<SearchWorkflowsQuery, PagedResult<WorkflowListItemDto>>
{
    public async Task<PagedResult<WorkflowListItemDto>> Handle(SearchWorkflowsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.Repository<Workflow>().AsNoTracking();

        var keyword = SearchNormalizer.Normalize(request.Keyword);
        if (keyword.Length > 0) query = query.Where(w => w.SearchText.Contains(keyword));
        if (request.IsActive is { } active) query = query.Where(w => w.IsActive == active);

        var total = await query.CountAsync(ct);
        var page = await query
            .OrderBy(w => w.OrderIndex).ThenBy(w => w.Code)
            .Skip((request.SafePage - 1) * request.SafePageSize)
            .Take(request.SafePageSize)
            .Select(w => new
            {
                w.Id, w.Code, w.Name, w.CategoryCode, w.CompanyCode, HasImage = w.ImagePath != null, w.OrderIndex, w.IsActive,
                w.CreatedName, w.CreatedDate
            })
            .ToListAsync(ct);

        // Cột "Mã trạng thái" = các mã nối bằng '-' theo thứ tự; một query cho cả trang (không N+1 như repository cũ).
        var ids = page.Select(w => w.Id).ToList();
        var statusCodes = (await unitOfWork.Repository<WorkflowStatus>().AsNoTracking()
                .Where(s => ids.Contains(s.WorkflowId))
                .OrderBy(s => s.OrderIndex)
                .Select(s => new { s.WorkflowId, s.Code })
                .ToListAsync(ct))
            .GroupBy(s => s.WorkflowId)
            .ToDictionary(g => g.Key, g => string.Join("-", g.Select(s => s.Code)));

        var items = page
            .Select(w => new WorkflowListItemDto(w.Id, w.Code, w.Name, w.CategoryCode, w.CompanyCode, w.HasImage, w.OrderIndex,
                w.IsActive, statusCodes.GetValueOrDefault(w.Id), w.CreatedName, w.CreatedDate))
            .ToList();
        return new PagedResult<WorkflowListItemDto>(items, total, request.SafePage, request.SafePageSize);
    }
}
