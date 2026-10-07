using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Domain.Common;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Kanbans.Queries.SearchKanbans;

/// <summary>Danh sách bảng Kanban: tìm mã/tên (không dấu), lọc trạng thái sử dụng; kèm số cột và số trạng thái đã xếp.</summary>
public sealed record SearchKanbansQuery : PagedQuery, IRequest<PagedResult<KanbanListItemDto>>
{
    public string? Keyword { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class SearchKanbansQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<SearchKanbansQuery, PagedResult<KanbanListItemDto>>
{
    public async Task<PagedResult<KanbanListItemDto>> Handle(SearchKanbansQuery request, CancellationToken ct)
    {
        var query = unitOfWork.Repository<Kanban>().AsNoTracking();
        var keyword = SearchNormalizer.Normalize(request.Keyword);
        if (keyword.Length > 0) query = query.Where(k => k.SearchText.Contains(keyword));
        if (request.IsActive is { } active) query = query.Where(k => k.IsActive == active);

        var columns = unitOfWork.Repository<KanbanColumn>().AsNoTracking();
        var mappings = unitOfWork.Repository<KanbanStatusMapping>().AsNoTracking();
        var projected = query
            .OrderBy(k => k.OrderIndex).ThenBy(k => k.Code)
            .Select(k => new KanbanListItemDto(
                k.Id, k.Code, k.Name, k.OrderIndex, k.IsActive,
                columns.Count(c => c.KanbanId == k.Id),
                mappings.Count(m => m.KanbanId == k.Id),
                k.CreatedName, k.CreatedDate));

        return await PagedResult<KanbanListItemDto>.CreateAsync(projected, request.SafePage, request.SafePageSize, ct);
    }
}
