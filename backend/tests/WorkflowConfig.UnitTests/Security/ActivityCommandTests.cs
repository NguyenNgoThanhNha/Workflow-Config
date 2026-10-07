using NSubstitute;
using WorkflowConfig.Application.Common.Exceptions;
using WorkflowConfig.Application.Common.Interfaces;
using WorkflowConfig.Application.Features.V1.Activities.Commands.DeleteActivity;
using WorkflowConfig.Application.Features.V1.Activities.Commands.SaveActivity;
using WorkflowConfig.Application.Features.V1.Roles.DTOs;
using WorkflowConfig.Application.Features.V1.Roles.Services;
using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.UnitTests.Security;

/// <summary>Chức năng tự thêm trên giao diện: thêm/sửa/xóa, khóa chức năng hệ thống, giữ quyền đã cấp nhất quán.</summary>
public class ActivityCommandTests
{
    private static SaveActivityCommandHandler Save(TestDb db, IPermissionService? perms = null) =>
        new(db.UnitOfWork, perms ?? Substitute.For<IPermissionService>());

    [Fact]
    public async Task Create_custom_activity_normalizes_action_order()
    {
        using var db = new TestDb();
        var dto = await Save(db).Handle(new SaveActivityCommand("EXPORT_EXCEL", " Xuất Excel ", null, "RC"), CancellationToken.None);

        Assert.Equal(("EXPORT_EXCEL", "Xuất Excel", "CR", false), (dto.Code, dto.Name, dto.Actions, dto.IsSystem));
    }

    [Fact]
    public async Task System_activity_cannot_be_edited_or_deleted()
    {
        using var db = new TestDb();
        var system = new SysActivity { Code = "WORKFLOW", Name = "Quy trình", IsSystem = true };
        db.Context.SysActivities.Add(system);
        db.Context.SaveChanges();

        await Assert.ThrowsAsync<ConflictException>(() =>
            Save(db).Handle(new SaveActivityCommand("WORKFLOW", "Đổi tên", null, "R") { Id = system.Id }, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() =>
            new DeleteActivityCommandHandler(db.UnitOfWork, Substitute.For<IPermissionService>())
                .Handle(new DeleteActivityCommand(system.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Removing_an_action_turns_that_flag_off_for_roles_and_invalidates_cache()
    {
        using var db = new TestDb();
        var perms = Substitute.For<IPermissionService>();
        var dto = await Save(db, perms).Handle(new SaveActivityCommand("EXPORT", "Xuất", null, "CR"), CancellationToken.None);
        var onlyCreate = db.AddRole("Chỉ tạo", null, (db.Context.SysActivities.Single(), "C"));
        var both = db.AddRole("Tạo + xem", null, (db.Context.SysActivities.Single(), "CR"));

        await Save(db, perms).Handle(new SaveActivityCommand("EXPORT", "Xuất", null, "R") { Id = dto.Id }, CancellationToken.None);

        Assert.Empty(db.Context.SysRoleActivities.Where(r => r.RoleId == onlyCreate.Id)); // không còn quyền nào → bỏ dòng
        var left = db.Context.SysRoleActivities.Single(r => r.RoleId == both.Id);
        Assert.Equal((false, true), (left.C, left.R));
        perms.Received().InvalidateAll();
    }

    [Fact]
    public async Task Delete_custom_activity_removes_granted_permissions()
    {
        using var db = new TestDb();
        var dto = await Save(db).Handle(new SaveActivityCommand("EXPORT", "Xuất", null, "R"), CancellationToken.None);
        db.AddRole("Xuất file excel", null, (db.Context.SysActivities.Single(), "R"));

        await new DeleteActivityCommandHandler(db.UnitOfWork, Substitute.For<IPermissionService>())
            .Handle(new DeleteActivityCommand(dto.Id), CancellationToken.None);

        Assert.Empty(db.Context.SysActivities);
        Assert.Empty(db.Context.SysRoleActivities);
    }

    [Fact]
    public async Task Role_permissions_cannot_enable_an_action_the_activity_does_not_use()
    {
        using var db = new TestDb();
        var dto = await Save(db).Handle(new SaveActivityCommand("EXPORT", "Xuất", null, "R"), CancellationToken.None);
        var validator = new ActivityPermissionInputsValidator(db.UnitOfWork);

        Assert.True((await validator.ValidateAsync([new ActivityPermissionInput(dto.Id, false, true, false, false)])).IsValid);
        Assert.False((await validator.ValidateAsync([new ActivityPermissionInput(dto.Id, true, true, false, false)])).IsValid);
    }
}
