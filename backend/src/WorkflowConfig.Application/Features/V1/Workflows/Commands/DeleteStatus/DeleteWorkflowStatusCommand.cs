using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.DeleteStatus;

/// <summary>Xóa trạng thái trên sơ đồ — bị chặn nếu còn bước chuyển đi ra hoặc đi vào.</summary>
public sealed record DeleteWorkflowStatusCommand(Guid WorkflowId, Guid StatusId) : IRequest;

public sealed class DeleteWorkflowStatusCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<DeleteWorkflowStatusCommand>
{
    public async Task Handle(DeleteWorkflowStatusCommand request, CancellationToken ct)
    {
        var status = await unitOfWork.Repository<WorkflowStatus>()
                         .FirstOrDefaultAsync(s => s.Id == request.StatusId && s.WorkflowId == request.WorkflowId, ct)
                     ?? throw new NotFoundException("Trạng thái", request.StatusId);

        if (await unitOfWork.Repository<StatusTransition>().AnyAsync(t => t.FromStatusId == status.Id || t.ToStatusId == status.Id, ct))
            throw new ConflictException($"Không thể xóa trạng thái \"{status.Name}\" vì đang có bước chuyển đi ra hoặc đi vào. Xóa các bước chuyển trước.");

        var rules = await unitOfWork.Repository<WorkflowStatusFieldRule>().Where(r => r.StatusId == status.Id).ToListAsync(ct);
        unitOfWork.Repository<WorkflowStatusFieldRule>().RemoveRange(rules);
        unitOfWork.Repository<WorkflowStatus>().Remove(status);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
