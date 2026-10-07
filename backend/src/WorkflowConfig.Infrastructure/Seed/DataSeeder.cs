using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkflowConfig.Application.Common.Interfaces;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Sys;
using WorkflowConfig.Persistence;

namespace WorkflowConfig.Infrastructure.Seed;

/// <summary>
/// Seed dữ liệu hệ thống (chạy mỗi lần khởi động, idempotent):
/// - Đồng bộ ConstActivity.All → Sys_Activity (thêm mã mới, cập nhật tên; không xóa mã cũ).
/// - Role hệ thống (Admin + ConstRole.DefaultPermissions) — quyền mặc định chỉ gán khi role mới được tạo.
/// - Tài khoản admin đầu tiên từ cấu hình "Seed:AdminEmail" / "Seed:AdminPassword" (nếu chưa có Admin nào).
/// - Danh mục quy trình (process, update mode, field) luôn đồng bộ; workflow mẫu chỉ ở môi trường dev — xem WorkflowSeed.
/// </summary>
public sealed class DataSeeder(
    IUnitOfWork<WorkflowConfigDbContext> unitOfWork,
    IPasswordHasher hasher,
    IConfiguration configuration,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(bool includeDemoData, CancellationToken ct = default)
    {
        var activities = await SyncActivitiesAsync(ct);
        var roles = await EnsureRolesAsync(activities, ct);
        await EnsureAdminAccountAsync(roles, ct);
        await WorkflowSeed.SyncCatalogsAsync(unitOfWork, ct);
        if (includeDemoData)
        {
            await WorkflowSeed.SeedDemoWorkflowAsync(unitOfWork, ct);
            await WorkflowSeed.SeedDemoKanbanAsync(unitOfWork, ct);
        }

        await unitOfWork.SaveChangesAsync(ct); // một lần, ở cuối
        logger.LogInformation("Data seeding completed");
    }

    private async Task<Dictionary<string, SysActivity>> SyncActivitiesAsync(CancellationToken ct)
    {
        var set = unitOfWork.Repository<SysActivity>();
        var existing = await set.ToDictionaryAsync(a => a.Code, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var def in ConstActivity.All)
        {
            if (existing.TryGetValue(def.Code, out var activity))
            {
                activity.Name = def.Name;
                activity.Description = def.Description;
                activity.Actions = def.Actions;
                activity.IsSystem = true;
            }
            else
            {
                activity = new SysActivity
                {
                    Code = def.Code, Name = def.Name, Description = def.Description, ApplicationName = ConstActivity.ApplicationName,
                    Actions = def.Actions, IsSystem = true
                };
                set.Add(activity);
                existing[def.Code] = activity;
            }
        }
        return existing;
    }

    private async Task<Dictionary<string, SysRole>> EnsureRolesAsync(Dictionary<string, SysActivity> activities, CancellationToken ct)
    {
        var set = unitOfWork.Repository<SysRole>();
        var roles = await set.ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase, ct);

        if (!roles.Values.Any(r => r.IsAdmin))
        {
            var admin = new SysRole { Name = ConstRole.Admin, Description = "Quản trị hệ thống — toàn quyền", RoleType = ConstRole.AdminRoleType };
            set.Add(admin);
            roles[admin.Name] = admin;
        }

        foreach (var (roleName, permissions) in ConstRole.DefaultPermissions)
        {
            if (roles.ContainsKey(roleName)) continue; // đã có → không ghi đè cấu hình Admin đã chỉnh

            var role = new SysRole { Name = roleName, Description = $"Role mặc định: {roleName}" };
            set.Add(role);
            roles[roleName] = role;

            foreach (var (code, flags) in permissions)
            {
                var ra = new SysRoleActivity { RoleId = role.Id, ActivityId = activities[code].Id };
                ra.SetFlags(flags.Contains('C'), flags.Contains('R'), flags.Contains('U'), flags.Contains('D'));
                unitOfWork.Repository<SysRoleActivity>().Add(ra);
            }
        }
        return roles;
    }

    private async Task EnsureAdminAccountAsync(Dictionary<string, SysRole> roles, CancellationToken ct)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var normalized = SysAccount.NormalizeEmail(email);
        if (await unitOfWork.Repository<SysAccount>().AnyAsync(u => u.Email == normalized, ct)) return;

        var admin = new SysAccount { Email = normalized, FullName = configuration["Seed:AdminFullName"] ?? "Administrator" };
        admin.SetPasswordHash(hasher.Hash(admin, password));
        unitOfWork.Repository<SysAccount>().Add(admin);

        var adminRole = roles.Values.First(r => r.IsAdmin);
        unitOfWork.Repository<SysUserRole>().Add(new SysUserRole { UserId = admin.Id, RoleId = adminRole.Id });
        logger.LogWarning("Seeded admin account {Email} — đổi mật khẩu ngay sau khi đăng nhập lần đầu", normalized);
    }
}
