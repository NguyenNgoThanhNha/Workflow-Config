using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Domain.Common;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanban;

public sealed record GetKanbanQuery(Guid Id) : IRequest<KanbanDetailDto>;

public sealed class GetKanbanQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork) : IRequestHandler<GetKanbanQuery, KanbanDetailDto>
{
    public Task<KanbanDetailDto> Handle(GetKanbanQuery request, CancellationToken ct) => KanbanDetailReader.ReadAsync(unitOfWork, request.Id, ct);
}

public static class KanbanDetailReader
{
    public static async Task<KanbanDetailDto> ReadAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, Guid id, CancellationToken ct)
    {
        var k = await unitOfWork.Repository<Kanban>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("Kanban", id);
        var columns = await ColumnsAsync(unitOfWork, id, ct);
        return new KanbanDetailDto(k.Id, k.Code, k.Name, k.OrderIndex, k.IsActive, k.RowVersion, k.CreatedName, k.CreatedDate,
            k.Updater, k.UpdatedDate, columns);
    }

    public static Task<List<KanbanColumnDto>> ColumnsAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, Guid kanbanId, CancellationToken ct) =>
        unitOfWork.Repository<KanbanColumn>().AsNoTracking()
            .Where(c => c.KanbanId == kanbanId)
            .OrderBy(c => c.OrderIndex).ThenBy(c => c.CreatedDate)
            .Select(c => new KanbanColumnDto(c.Id, c.Name, c.OrderIndex, c.Note, c.Color))
            .ToListAsync(ct);
}
