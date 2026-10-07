using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Domain.Common;
using WorkflowConfig.Domain.Entities.Workflows;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanban;

namespace WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanbanBoard;

/// <summary>
/// Bảng cấu hình Kanban: các cột + mọi trạng thái của workflow đang dùng (lọc theo WorkflowId nếu có).
/// Trạng thái chưa xếp vào cột nào có ColumnId = null (cột "Chưa cấu hình" trên giao diện).
/// </summary>
public sealed record GetKanbanBoardQuery(Guid Id, Guid? WorkflowId) : IRequest<KanbanBoardDto>;

public sealed class GetKanbanBoardQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetKanbanBoardQuery, KanbanBoardDto>
{
    private const string DefaultBackground = "#FFFFFF";
    private const string DefaultText = "#000000";

    public async Task<KanbanBoardDto> Handle(GetKanbanBoardQuery request, CancellationToken ct)
    {
        var kanban = await unitOfWork.Repository<Kanban>().AsNoTracking()
            .Where(k => k.Id == request.Id)
            .Select(k => new { k.Id, k.Code, k.Name })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Kanban", request.Id);

        var columns = await KanbanDetailReader.ColumnsAsync(unitOfWork, request.Id, ct);

        var workflows = await unitOfWork.Repository<Workflow>().AsNoTracking()
            .Where(w => w.IsActive && unitOfWork.Repository<WorkflowStatus>().Any(s => s.WorkflowId == w.Id))
            .OrderBy(w => w.OrderIndex).ThenBy(w => w.Code)
            .Select(w => new KanbanWorkflowOptionDto(w.Id, w.Code, w.Name))
            .ToListAsync(ct);

        var cards = await (
                from s in unitOfWork.Repository<WorkflowStatus>().AsNoTracking()
                join w in unitOfWork.Repository<Workflow>().AsNoTracking() on s.WorkflowId equals w.Id
                where w.IsActive && (request.WorkflowId == null || w.Id == request.WorkflowId)
                join p in unitOfWork.Repository<WorkflowProcess>().AsNoTracking() on s.ProcessCode equals p.Code into pj
                from p in pj.DefaultIfEmpty()
                join m in unitOfWork.Repository<KanbanStatusMapping>().AsNoTracking().Where(x => x.KanbanId == request.Id)
                    on s.Id equals m.StatusId into mj
                from m in mj.DefaultIfEmpty()
                orderby w.OrderIndex, w.Code, s.OrderIndex
                select new
                {
                    StatusId = s.Id, WorkflowId = w.Id, WorkflowCode = w.Code, WorkflowName = w.Name, StatusCode = s.Code,
                    StatusName = s.Name, s.ProcessCode, s.TextColor, s.BackgroundColor,
                    ProcessBackground = p != null ? p.BackgroundColor : null,
                    ProcessText = p != null ? p.TextColor : null,
                    ColumnId = m != null ? m.ColumnId : (Guid?)null
                })
            .ToListAsync(ct);

        var columnIds = columns.Select(c => c.Id).ToHashSet();
        var cardDtos = cards.Select(c =>
        {
            var ownColor = !string.IsNullOrEmpty(c.TextColor);
            return new KanbanCardDto(c.StatusId, c.WorkflowId, c.WorkflowCode, c.WorkflowName, c.StatusCode, c.StatusName, c.ProcessCode,
                (ownColor ? c.BackgroundColor : c.ProcessBackground) ?? DefaultBackground,
                (ownColor ? c.TextColor : c.ProcessText) ?? DefaultText,
                c.ColumnId is { } id && columnIds.Contains(id) ? id : null);
        }).ToList();

        return new KanbanBoardDto(kanban.Id, kanban.Code, kanban.Name, columns, workflows, cardDtos);
    }
}
