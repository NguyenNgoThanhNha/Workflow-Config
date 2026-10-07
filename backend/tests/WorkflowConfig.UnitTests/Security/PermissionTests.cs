using WorkflowConfig.Application.Common.Security;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Sys;
using WorkflowConfig.Domain.Enums;
using WorkflowConfig.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace WorkflowConfig.UnitTests.Security;

/// <summary>Logic gộp quyền 6 bảng: Admin toàn quyền; OR(quyền role, quyền riêng); tài khoản khóa không có quyền.</summary>
public class PermissionTests
{
    private static PermissionService CreateService(TestDb db, PermissionCacheVersion? version = null) =>
        new(db.UnitOfWork, new MemoryCache(new MemoryCacheOptions()), version ?? new PermissionCacheVersion());

    [Fact]
    public async Task Role_and_user_permissions_are_merged_with_OR()
    {
        using var db = new TestDb();
        var act = db.SeedActivities();
        var agent = db.AddRole("Editor", null, (act[ConstActivity.Role], "CR"));
        var user = db.AddUser("u@x.vn", true, agent);
        var own = new SysUserActivity { UserId = user.Id, ActivityId = act[ConstActivity.Role].Id };
        own.SetFlags(false, false, true, false);
        db.Context.SysUserActivities.Add(own);
        db.Context.SaveChanges();

        var effective = await CreateService(db).GetEffectiveAsync(user.Id);

        Assert.True(effective.Has(ConstActivity.Role, ActivityType.Create));
        Assert.True(effective.Has(ConstActivity.Role, ActivityType.Update)); // từ quyền riêng
        Assert.False(effective.Has(ConstActivity.Role, ActivityType.Delete));
        Assert.False(effective.Has(ConstActivity.ApiLog, ActivityType.Read));
    }

    [Fact]
    public async Task Admin_role_has_every_permission()
    {
        using var db = new TestDb();
        db.SeedActivities();
        var admin = db.AddRole(ConstRole.Admin, ConstRole.AdminRoleType);
        var user = db.AddUser("admin@x.vn", true, admin);

        var effective = await CreateService(db).GetEffectiveAsync(user.Id);

        Assert.True(effective.IsAdmin);
        Assert.All(ConstActivity.All, a => Assert.True(effective.Has(a.Code, ActivityType.Delete)));
    }

    [Fact]
    public async Task Locked_account_has_no_permission()
    {
        using var db = new TestDb();
        db.SeedActivities();
        var admin = db.AddRole(ConstRole.Admin, ConstRole.AdminRoleType);
        var user = db.AddUser("locked@x.vn", false, admin);

        var effective = await CreateService(db).GetEffectiveAsync(user.Id);

        Assert.False(effective.Has(ConstActivity.Role, ActivityType.Read));
    }

    [Fact]
    public async Task Soft_deleted_role_link_is_ignored()
    {
        using var db = new TestDb();
        var act = db.SeedActivities();
        var qa = db.AddRole("QA", null, (act[ConstActivity.ApiLog], "R"));
        var user = db.AddUser("qa@x.vn", true, qa);
        db.Context.SysUserRoles.Remove(db.Context.SysUserRoles.Single(ur => ur.UserId == user.Id)); // → xóa mềm
        db.Context.SaveChanges();

        var effective = await CreateService(db).GetEffectiveAsync(user.Id);

        Assert.False(effective.Has(ConstActivity.ApiLog, ActivityType.Read));
    }

    [Fact]
    public async Task Invalidate_all_forces_recompute_after_role_change()
    {
        using var db = new TestDb();
        var act = db.SeedActivities();
        var role = db.AddRole("Viewer", null, (act[ConstActivity.ApiLog], "R"));
        var user = db.AddUser("v@x.vn", true, role);
        var service = CreateService(db);
        Assert.True((await service.GetEffectiveAsync(user.Id)).Has(ConstActivity.ApiLog, ActivityType.Read));

        db.Context.SysRoleActivities.Single().SetFlags(false, false, false, false);
        db.Context.SaveChanges();
        Assert.True((await service.GetEffectiveAsync(user.Id)).Has(ConstActivity.ApiLog, ActivityType.Read)); // còn cache

        service.InvalidateAll();
        Assert.False((await service.GetEffectiveAsync(user.Id)).Has(ConstActivity.ApiLog, ActivityType.Read));
    }

    [Theory]
    [InlineData("TICKET:C", "TICKET", ActivityType.Create)]
    [InlineData("user:r", "USER", ActivityType.Read)]
    public void PermissionKey_parses(string key, string code, ActivityType type)
    {
        Assert.True(PermissionKey.TryParse(key, out var parsedCode, out var parsedType));
        Assert.Equal(code, parsedCode);
        Assert.Equal(type, parsedType);
    }

    [Theory]
    [InlineData("TICKET")]
    [InlineData("TICKET:X")]
    [InlineData(":C")]
    public void PermissionKey_rejects_invalid(string key) => Assert.False(PermissionKey.TryParse(key, out _, out _));

    [Fact]
    public void Effective_permissions_require_active_account()
    {
        var flags = new Dictionary<string, PermissionFlags> { [ConstActivity.Role] = PermissionFlags.All };

        Assert.True(new EffectivePermissions(Guid.NewGuid(), true, false, [], flags).Has(ConstActivity.Role, ActivityType.Delete));
        Assert.False(new EffectivePermissions(Guid.NewGuid(), false, true, [], flags).Has(ConstActivity.Role, ActivityType.Read));
    }

    [Fact]
    public void Remove_on_BaseEntity_is_soft_delete_and_hidden_by_query_filter()
    {
        using var db = new TestDb();
        var role = db.AddRole("Temp");

        db.Context.SysRoles.Remove(role);
        db.Context.SaveChanges();

        Assert.Empty(db.Context.SysRoles);
        Assert.True(db.Context.SysRoles.IgnoreQueryFilters().Single().IsDeleted);
    }
}
