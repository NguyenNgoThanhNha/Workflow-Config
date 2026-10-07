using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.Application.Features.V1.Activities.Commands.DeleteActivity;

/// <summary>Xóa chức năng tự định nghĩa cùng các quyền đã cấp cho role / user. Chức năng hệ thống không xóa được.</summary>
public sealed record DeleteActivityCommand(Guid Id) : IRequest;

public sealed class DeleteActivityCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, IPermissionService permissionService)
    : IRequestHandler<DeleteActivityCommand>
{
    public async Task Handle(DeleteActivityCommand request, CancellationToken ct)
    {
        var activity = await unitOfWork.Repository<SysActivity>().FirstOrDefaultAsync(a => a.Id == request.Id, ct)
                       ?? throw new NotFoundException("Chức năng", request.Id);
        if (activity.IsSystem) throw new ConflictException("Chức năng hệ thống được khai trong code, không xóa trên giao diện.");

        unitOfWork.Repository<SysRoleActivity>().RemoveRange(
            await unitOfWork.Repository<SysRoleActivity>().Where(x => x.ActivityId == activity.Id).ToListAsync(ct));
        unitOfWork.Repository<SysUserActivity>().RemoveRange(
            await unitOfWork.Repository<SysUserActivity>().Where(x => x.ActivityId == activity.Id).ToListAsync(ct));
        unitOfWork.Repository<SysActivity>().Remove(activity);

        await unitOfWork.SaveChangesAsync(ct);
        permissionService.InvalidateAll();
    }
}
