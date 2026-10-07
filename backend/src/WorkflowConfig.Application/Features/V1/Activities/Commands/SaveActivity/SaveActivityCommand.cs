using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Roles.DTOs;
using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.Application.Features.V1.Activities.Commands.SaveActivity;

/// <summary>
/// Thêm (Id = null) hoặc sửa chức năng tự định nghĩa trên giao diện. Chức năng hệ thống (khai trong code) không sửa được.
/// Actions: các quyền áp dụng, chuỗi con của "CRUD".
/// </summary>
public sealed record SaveActivityCommand(string Code, string Name, string? Description, string Actions) : IRequest<ActivityDto>
{
    [JsonIgnore]
    public Guid? Id { get; init; }
}

public sealed class SaveActivityCommandValidator : AbstractValidator<SaveActivityCommand>
{
    public SaveActivityCommandValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z][A-Z0-9_]*$").WithMessage("Mã chỉ gồm chữ in hoa, số và dấu gạch dưới, bắt đầu bằng chữ (vd EXPORT_EXCEL).")
            .MustAsync(async (cmd, code, ct) => !await unitOfWork.Repository<SysActivity>()
                .AnyAsync(a => a.Code == code && a.Id != (cmd.Id ?? Guid.Empty), ct))
            .WithMessage("Mã chức năng đã tồn tại.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Actions)
            .NotEmpty().WithMessage("Chọn ít nhất một quyền.")
            .Must(a => a.All(ConstActivity.AllActions.Contains) && a.Distinct().Count() == a.Length)
            .WithMessage("Quyền chỉ gồm C, R, U, D.");
    }
}

public sealed class SaveActivityCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, IPermissionService permissionService)
    : IRequestHandler<SaveActivityCommand, ActivityDto>
{
    public async Task<ActivityDto> Handle(SaveActivityCommand request, CancellationToken ct)
    {
        var set = unitOfWork.Repository<SysActivity>();
        SysActivity activity;
        if (request.Id is { } id)
        {
            activity = await set.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new NotFoundException("Chức năng", id);
            if (activity.IsSystem) throw new ConflictException("Chức năng hệ thống được khai trong code, không sửa trên giao diện.");
        }
        else
        {
            activity = new SysActivity { Code = request.Code, Name = request.Name, ApplicationName = ConstActivity.ApplicationName };
            set.Add(activity);
        }

        var removedActions = activity.Actions.Except(request.Actions).ToHashSet();
        activity.Code = request.Code;
        activity.Name = request.Name.Trim();
        activity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        // giữ thứ tự chuẩn C-R-U-D
        activity.Actions = new string(ConstActivity.AllActions.Where(request.Actions.Contains).ToArray());

        if (request.Id is not null && removedActions.Count > 0) await ClearRemovedFlagsAsync(activity.Id, removedActions, ct);

        await unitOfWork.SaveChangesAsync(ct);
        if (removedActions.Count > 0) permissionService.InvalidateAll();
        return new ActivityDto(activity.Id, activity.Code, activity.Name, activity.Description, activity.Actions, activity.IsSystem);
    }

    /// <summary>Bỏ một quyền khỏi chức năng → tắt cờ đó ở mọi role / user đang có.</summary>
    private async Task ClearRemovedFlagsAsync(Guid activityId, HashSet<char> removed, CancellationToken ct)
    {
        void Clear<T>(DbSet<T> set, T p) where T : CrudPermission
        {
            p.SetFlags(p.C && !removed.Contains('C'), p.R && !removed.Contains('R'), p.U && !removed.Contains('U'), p.D && !removed.Contains('D'));
            if (p.IsEmpty) set.Remove(p); // không còn quyền nào → bỏ dòng
        }

        var roles = unitOfWork.Repository<SysRoleActivity>();
        foreach (var p in await roles.Where(x => x.ActivityId == activityId).ToListAsync(ct)) Clear(roles, p);
        var users = unitOfWork.Repository<SysUserActivity>();
        foreach (var p in await users.Where(x => x.ActivityId == activityId).ToListAsync(ct)) Clear(users, p);
    }
}
