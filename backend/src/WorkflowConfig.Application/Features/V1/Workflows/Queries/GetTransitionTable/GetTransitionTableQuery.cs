using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetTransitionTable;

/// <summary>
/// Bảng cấu hình bước chuyển dạng danh sách (màn Config cũ): mỗi trạng thái đang dùng × mỗi bước chuyển đi ra
/// (trạng thái chưa có bước chuyển vẫn có một dòng).
/// </summary>
public sealed record GetTransitionTableQuery(Guid WorkflowId) : IRequest<IReadOnlyList<TransitionTableRowDto>>;

public sealed class GetTransitionTableQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetTransitionTableQuery, IReadOnlyList<TransitionTableRowDto>>
{
    public async Task<IReadOnlyList<TransitionTableRowDto>> Handle(GetTransitionTableQuery request, CancellationToken ct)
    {
        if (!await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Id == request.WorkflowId, ct))
            throw new NotFoundException("Workflow", request.WorkflowId);

        var rows = await (
                from s in unitOfWork.Repository<WorkflowStatus>().AsNoTracking()
                where s.WorkflowId == request.WorkflowId && s.IsActive
                join p in unitOfWork.Repository<WorkflowProcess>().AsNoTracking() on s.ProcessCode equals p.Code into pj
                from p in pj.DefaultIfEmpty()
                join t in unitOfWork.Repository<StatusTransition>().AsNoTracking() on s.Id equals t.FromStatusId into tj
                from t in tj.DefaultIfEmpty()
                join to in unitOfWork.Repository<WorkflowStatus>().AsNoTracking() on t.ToStatusId equals to.Id into toj
                from to in toj.DefaultIfEmpty()
                orderby s.OrderIndex, t.OrderIndex
                select new
                {
                    StatusId = s.Id, StatusName = s.Name, ProcessName = p != null ? p.Name : null, s.OrderIndex,
                    TransitionId = t != null ? t.Id : (Guid?)null, TransitionName = t != null ? t.Name : null,
                    ToStatusName = to != null ? to.Name : null
                })
            .ToListAsync(ct);

        return rows.Select(r => new TransitionTableRowDto(r.StatusId, r.StatusName, r.ProcessName, r.OrderIndex, r.TransitionId,
            r.TransitionName, r.ToStatusName)).ToList();
    }
}
