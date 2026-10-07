using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.DeleteTransition;

/// <summary>Xóa bước chuyển (DeleteTransition / DeleteStatusTransition cũ) cùng điều kiện tự động và cấu hình thông báo.</summary>
public sealed record DeleteTransitionCommand(Guid WorkflowId, Guid TransitionId) : IRequest;

public sealed class DeleteTransitionCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<DeleteTransitionCommand>
{
    public async Task Handle(DeleteTransitionCommand request, CancellationToken ct)
    {
        var transition = await unitOfWork.Repository<StatusTransition>()
                             .FirstOrDefaultAsync(t => t.Id == request.TransitionId && t.WorkflowId == request.WorkflowId, ct)
                         ?? throw new NotFoundException("Bước chuyển", request.TransitionId);

        var notifications = await unitOfWork.Repository<TransitionNotification>().Where(n => n.TransitionId == transition.Id).ToListAsync(ct);
        var notificationIds = notifications.Select(n => n.Id).ToList();

        unitOfWork.Repository<NotificationRecipient>().RemoveRange(
            await unitOfWork.Repository<NotificationRecipient>().Where(r => notificationIds.Contains(r.NotificationId)).ToListAsync(ct));
        unitOfWork.Repository<NotificationAttachment>().RemoveRange(
            await unitOfWork.Repository<NotificationAttachment>().Where(a => notificationIds.Contains(a.NotificationId)).ToListAsync(ct));
        unitOfWork.Repository<TransitionNotification>().RemoveRange(notifications);
        unitOfWork.Repository<AutoCondition>().RemoveRange(
            await unitOfWork.Repository<AutoCondition>().Where(c => c.TransitionId == transition.Id).ToListAsync(ct));
        unitOfWork.Repository<StatusTransition>().Remove(transition);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
