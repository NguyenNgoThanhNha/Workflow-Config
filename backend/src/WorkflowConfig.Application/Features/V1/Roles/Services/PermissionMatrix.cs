using FluentValidation;
using WorkflowConfig.Application.Features.V1.Roles.DTOs;
using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.Application.Features.V1.Roles.Services;

/// <summary>
/// Ghi ma trận Activity × C/R/U/D — dùng chung cho quyền của role (Sys_RoleActivity)
/// và quyền riêng của user (Sys_UserActivity).
/// </summary>
public static class PermissionMatrix
{
    /// <summary>
    /// Đồng bộ <paramref name="existing"/> (các dòng đang có, đã tracking) theo <paramref name="inputs"/>:
    /// cập nhật cờ, thêm dòng mới, xóa mềm dòng không còn quyền nào.
    /// </summary>
    public static void Apply<T>(
        IReadOnlyCollection<T> existing,
        IEnumerable<ActivityPermissionInput> inputs,
        Func<Guid, T> create,
        DbSet<T> set) where T : CrudPermission
    {
        var wanted = inputs.Where(i => i.Any).GroupBy(i => i.ActivityId).ToDictionary(g => g.Key, g => g.Last());
        var byActivity = existing.ToDictionary(e => e.ActivityId);

        foreach (var (activityId, input) in wanted)
        {
            if (!byActivity.TryGetValue(activityId, out var row))
            {
                row = create(activityId);
                set.Add(row);
            }
            row.SetFlags(input.C, input.R, input.U, input.D);
        }

        foreach (var row in existing.Where(e => !wanted.ContainsKey(e.ActivityId)))
            set.Remove(row); // → xóa mềm (AuditSaveChangesInterceptor)
    }

    public static async Task<IReadOnlyList<ActivityPermissionDto>> ToDtosAsync(
        IUnitOfWork<WorkflowConfigDbContext> unitOfWork, IEnumerable<CrudPermission> rows, CancellationToken ct)
    {
        var activities = await unitOfWork.Repository<SysActivity>().AsNoTracking().ToDictionaryAsync(a => a.Id, ct);
        return rows.Where(r => activities.ContainsKey(r.ActivityId))
            .Select(r => new ActivityPermissionDto(r.ActivityId, activities[r.ActivityId].Code, activities[r.ActivityId].Name, r.C, r.R, r.U, r.D))
            .OrderBy(d => d.Code)
            .ToList();
    }
}

public sealed class ActivityPermissionInputsValidator : AbstractValidator<IReadOnlyList<ActivityPermissionInput>>
{
    public ActivityPermissionInputsValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x)
            .MustAsync(async (inputs, ct) =>
            {
                var ids = inputs.Select(i => i.ActivityId).Distinct().ToList();
                if (ids.Count == 0) return true;
                return await unitOfWork.Repository<SysActivity>().CountAsync(a => ids.Contains(a.Id), ct) == ids.Count;
            })
            .OverridePropertyName("activities")
            .WithMessage("Có activity không tồn tại.");

        // Không cho bật quyền mà chức năng không dùng (vd "Thêm" cho chức năng chỉ có "Xem").
        RuleFor(x => x)
            .MustAsync(async (inputs, ct) =>
            {
                var ids = inputs.Select(i => i.ActivityId).Distinct().ToList();
                if (ids.Count == 0) return true;
                var actions = await unitOfWork.Repository<SysActivity>().AsNoTracking()
                    .Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Actions, ct);
                return inputs.All(i => !actions.TryGetValue(i.ActivityId, out var allowed) ||
                    ((!i.C || allowed.Contains('C')) && (!i.R || allowed.Contains('R')) &&
                     (!i.U || allowed.Contains('U')) && (!i.D || allowed.Contains('D'))));
            })
            .OverridePropertyName("activities")
            .WithMessage("Có quyền không áp dụng cho chức năng đã chọn.");
    }
}
