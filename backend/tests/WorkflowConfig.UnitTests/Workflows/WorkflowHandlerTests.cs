using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkflowConfig.Application.Common.Exceptions;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.CopyWorkflow;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.DeleteStatus;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveStatus;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveTransition;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveWorkflow;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.UpdateNodePosition;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowDiagram;
using WorkflowConfig.Application.Features.V1.Workflows.Services;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.UnitTests.Workflows;

public class WorkflowHandlerTests
{
    private static SaveTransitionCommand Transition(Guid from, Guid to, string name = "Duyệt") => new(
        name, null, null, null, from, to, ConstWorkflow.Anchor.Right, ConstWorkflow.Anchor.Left, null, null, null,
        false, true, false, false, false, false, false, null, false,
        ConstWorkflow.UpdateMode.NotConfig, null, null, ConstWorkflow.UpdateMode.NotConfig, null, null,
        ConstWorkflow.Signature.None, null, [], []);

    private static TransitionNotificationInput Notification(string type) => new(
        null, type, null, null, null, "ZNS-1", true, null, null, true, true, false, "Tiêu đề", "Nội dung",
        [new NotificationRecipientInput(null, ConstWorkflow.UpdateMode.Roles, "x")], [], [new NotificationAttachmentInput(null, "a.pdf")]);

    private static ICrmMetadataReader Metadata()
    {
        var reader = Substitute.For<ICrmMetadataReader>();
        reader.GetSchemaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("dbo");
        return reader;
    }

    // ---------- Workflow ----------

    [Fact]
    public async Task Create_saves_statuses_and_only_chosen_fields()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var handler = new SaveWorkflowCommandHandler(db.UnitOfWork);

        var result = await handler.Handle(new SaveWorkflowCommand(
            " WF_NEW ", "Quy trình mới", "NV", "1000", 3, null, false,
            [new WorkflowStatusInput(null, "NEW", "Mới", 1, null, ConstWorkflow.Process.Todo),
             new WorkflowStatusInput(null, "DONE", "Xong", 2, "CAT", ConstWorkflow.Process.Completed)],
            [new WorkflowFieldConfigInput("Summary", true, 1, null, "Tiêu đề", "Title", false, null, false, null)],
            null), CancellationToken.None);

        Assert.Equal("WF_NEW", result.Code);
        Assert.True(result.IsActive); // không gửi IsActive → mặc định đang sử dụng
        Assert.Equal(["NEW", "DONE"], result.Statuses.Select(s => s.Code));
        var summary = Assert.Single(result.Fields, f => f.IsChosen);
        Assert.Equal(("Summary", true, "Title"), (summary.FieldCode, summary.IsRequired, summary.NoteEn));
        Assert.Contains(result.Fields, f => f.FieldCode == "Description" && !f.IsChosen); // field chưa chọn vẫn liệt kê
        Assert.Equal("wf_new quy trinh moi", db.Context.Set<Workflow>().Single().SearchText);
    }

    [Fact]
    public async Task Update_cannot_drop_a_status_that_still_has_transitions()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        WorkflowTestData.AddTransition(db, wf, wf.New, wf.Approved, "Duyệt");
        var handler = new SaveWorkflowCommandHandler(db.UnitOfWork);

        // bỏ trạng thái APPROVED khỏi form
        var command = new SaveWorkflowCommand(wf.Workflow.Code, wf.Workflow.Name, "NV", "1000", 1, true, false,
            [new WorkflowStatusInput(wf.New.Id, "NEW", "NEW", 1, null, ConstWorkflow.Process.Todo),
             new WorkflowStatusInput(wf.Rejected.Id, "REJECTED", "REJECTED", 3, null, ConstWorkflow.Process.Completed)],
            [], wf.Workflow.RowVersion) { Id = wf.Workflow.Id };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("APPROVED", ex.Message);
    }

    [Fact]
    public async Task Update_removes_unticked_fields_and_free_statuses()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var handler = new SaveWorkflowCommandHandler(db.UnitOfWork);

        var result = await handler.Handle(new SaveWorkflowCommand(wf.Workflow.Code, "Tên mới", "NV", "1000", 1, false, true,
            [new WorkflowStatusInput(wf.New.Id, "NEW", "Mới tạo", 1, null, ConstWorkflow.Process.Todo)],
            [new WorkflowFieldConfigInput("Description", false, 1, null, null, null, false, null, false, null)],
            wf.Workflow.RowVersion) { Id = wf.Workflow.Id }, CancellationToken.None);

        Assert.Equal(("Tên mới", false, true), (result.Name, result.IsActive, result.IsSummaryDisabled));
        Assert.Equal("Mới tạo", Assert.Single(result.Statuses).Name);
        Assert.Equal("Description", Assert.Single(result.Fields, f => f.IsChosen).FieldCode);
        // xóa mềm: bản ghi cũ vẫn còn nhưng bị ẩn
        Assert.Equal(3, db.Context.Set<WorkflowStatus>().IgnoreQueryFilters().Count());
    }

    [Fact]
    public async Task Copy_clones_the_whole_graph_with_new_ids()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var t = WorkflowTestData.AddTransition(db, wf, wf.New, wf.Approved, "Duyệt", "Kết quả");
        db.Context.Set<AutoCondition>().Add(new AutoCondition { TransitionId = t.Id, ConditionType = "FIELD", Field = "Summary", ComparisonType = "=" });
        var noti = new TransitionNotification { TransitionId = t.Id, Type = ConstWorkflow.NotificationType.Email };
        db.Context.Set<TransitionNotification>().Add(noti);
        db.Context.Set<NotificationRecipient>().Add(new NotificationRecipient { NotificationId = noti.Id, Kind = ConstWorkflow.RecipientKind.Cc });
        db.Context.Set<WorkflowStatusFieldRule>().Add(new WorkflowStatusFieldRule
        {
            WorkflowId = wf.Workflow.Id, StatusId = wf.Approved.Id, FieldCode = "Summary", RequiredForAssignee = true
        });
        db.Context.SaveChanges();

        var copied = await new CopyWorkflowCommandHandler(db.UnitOfWork)
            .Handle(new CopyWorkflowCommand("WF_COPY", "Bản copy", 5) { SourceId = wf.Workflow.Id }, CancellationToken.None);

        var statuses = db.Context.Set<WorkflowStatus>().Where(s => s.WorkflowId == copied.Id).ToList();
        Assert.Equal(3, statuses.Count);
        Assert.DoesNotContain(statuses, s => s.Id == wf.New.Id);
        var copiedTransition = Assert.Single(db.Context.Set<StatusTransition>().Where(x => x.WorkflowId == copied.Id));
        Assert.Equal(statuses.Single(s => s.Code == "NEW").Id, copiedTransition.FromStatusId);
        Assert.Equal(statuses.Single(s => s.Code == "APPROVED").Id, copiedTransition.ToStatusId);
        Assert.Equal("KET_QUA", copiedTransition.BranchKey);
        Assert.Single(db.Context.Set<AutoCondition>().Where(c => c.TransitionId == copiedTransition.Id));
        var copiedNoti = Assert.Single(db.Context.Set<TransitionNotification>().Where(n => n.TransitionId == copiedTransition.Id));
        Assert.Single(db.Context.Set<NotificationRecipient>().Where(r => r.NotificationId == copiedNoti.Id));
        var rule = Assert.Single(db.Context.Set<WorkflowStatusFieldRule>().Where(r => r.WorkflowId == copied.Id));
        Assert.Equal(statuses.Single(s => s.Code == "APPROVED").Id, rule.StatusId);
        Assert.Single(db.Context.Set<WorkflowFieldConfig>().Where(f => f.WorkflowId == copied.Id));
    }

    [Fact]
    public async Task Copy_requires_code_and_name_different_from_source()
    {
        using var db = new TestDb();
        var wf = WorkflowTestData.AddWorkflow(db);

        await Assert.ThrowsAsync<ValidationException>(() => new CopyWorkflowCommandHandler(db.UnitOfWork)
            .Handle(new CopyWorkflowCommand("wf1", "Khác", 1) { SourceId = wf.Workflow.Id }, CancellationToken.None));
    }

    // ---------- Trạng thái ----------

    [Fact]
    public async Task Status_with_transitions_cannot_be_deleted()
    {
        using var db = new TestDb();
        var wf = WorkflowTestData.AddWorkflow(db);
        WorkflowTestData.AddTransition(db, wf, wf.New, wf.Approved, "Duyệt");
        var handler = new DeleteWorkflowStatusCommandHandler(db.UnitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeleteWorkflowStatusCommand(wf.Workflow.Id, wf.Approved.Id), CancellationToken.None));
        await handler.Handle(new DeleteWorkflowStatusCommand(wf.Workflow.Id, wf.Rejected.Id), CancellationToken.None);
        Assert.Equal(2, db.Context.Set<WorkflowStatus>().Count());
    }

    [Fact]
    public async Task Status_field_rules_keep_only_rows_with_a_flag_and_push_off_clears_recipients()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var handler = new SaveWorkflowStatusCommandHandler(db.UnitOfWork);

        await handler.Handle(new SaveWorkflowStatusCommand("NEW", "Mới", 1, null, ConstWorkflow.Process.Todo, "#000000", "#FFFFFF", null,
            true, false, true, true, false, "t", "m",
            [new StatusFieldRuleInput("Summary", true, false, false, false, false, false),
             new StatusFieldRuleInput("Description", false, false, false, false, false, false)])
        { WorkflowId = wf.Workflow.Id, StatusId = wf.New.Id }, CancellationToken.None);

        var status = db.Context.Set<WorkflowStatus>().Single(s => s.Id == wf.New.Id);
        Assert.True(status.AutoUpdateEndDate);
        Assert.False(status.IsSendCreator); // push tắt → không giữ người nhận / nội dung
        Assert.Null(status.NotificationTitle);
        Assert.Equal("Summary", Assert.Single(db.Context.Set<WorkflowStatusFieldRule>()).FieldCode);
    }

    // ---------- Bước chuyển ----------

    [Fact]
    public async Task Transition_normalizes_sign_and_update_modes_and_drops_conditions_when_not_automatic()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var handler = new SaveTransitionCommandHandler(db.UnitOfWork, Metadata());

        var id = await handler.Handle(Transition(wf.New.Id, wf.Approved.Id) with
        {
            SignatureType = ConstWorkflow.Signature.None,
            SignerType = ConstWorkflow.Signature.SignerUnit,
            AssigneeUpdateMode = ConstWorkflow.UpdateMode.Department,
            AssigneeRoleId = Guid.NewGuid(),
            AssigneeValue = " PB01 ",
            ReporterUpdateMode = ConstWorkflow.UpdateMode.NotConfig,
            ReporterValue = "rác",
            BranchName = " Duyệt cấp 1 ",
            IsAutomatic = false,
            Conditions = [new AutoConditionInput(null, null, "FIELD", "Summary", "=", null, "'x'", null)],
            WorkflowId = wf.Workflow.Id
        }, CancellationToken.None);

        var t = db.Context.Set<StatusTransition>().Single(x => x.Id == id);
        Assert.Null(t.SignerType); // không ký → bỏ người ký
        Assert.Equal((ConstWorkflow.UpdateMode.Department, (Guid?)null, "PB01"), (t.AssigneeUpdateMode, t.AssigneeRoleId, t.AssigneeValue));
        Assert.Null(t.ReporterValue);
        Assert.Equal(("Duyệt cấp 1", "DUYET_CAP_1"), (t.BranchName, t.BranchKey));
        Assert.Empty(db.Context.Set<AutoCondition>());
    }

    [Fact]
    public async Task Transition_conditions_build_sql_text_and_are_synced_by_id()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var handler = new SaveTransitionCommandHandler(db.UnitOfWork, Metadata());
        var create = Transition(wf.New.Id, wf.Approved.Id) with
        {
            IsAutomatic = true,
            Conditions =
            [
                new AutoConditionInput(null, null, "FIELD", "Summary", "=", null, "'a'", null),
                new AutoConditionInput(null, "AND", "TIME", "GETDATE()", ">", "INPUT", "1", "tự viết")
            ],
            WorkflowId = wf.Workflow.Id
        };
        var id = await handler.Handle(create, CancellationToken.None);

        var conditions = db.Context.Set<AutoCondition>().OrderBy(c => c.OrderIndex).ToList();
        Assert.Equal(["Summary='a'", "tự viết"], conditions.Select(c => c.SqlText));
        Assert.Equal(ConstWorkflow.Condition.ValueInput, conditions[0].ValueType);

        // sửa: giữ dòng 2 (theo Id), bỏ dòng 1
        await handler.Handle(create with
        {
            Conditions = [new AutoConditionInput(conditions[1].Id, null, "TIME", "GETDATE()", "<", "API", "2", null)],
            TransitionId = id
        }, CancellationToken.None);

        var kept = Assert.Single(db.Context.Set<AutoCondition>());
        Assert.Equal(conditions[1].Id, kept.Id);
        Assert.Equal(("GETDATE()<2", 0), (kept.SqlText, kept.OrderIndex));
    }

    [Theory]
    [InlineData(ConstWorkflow.NotificationType.Push)]
    [InlineData(ConstWorkflow.NotificationType.Zalo)]
    [InlineData(ConstWorkflow.NotificationType.Email)]
    public async Task Notification_keeps_only_fields_of_its_type(string type)
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var handler = new SaveTransitionCommandHandler(db.UnitOfWork, Metadata());

        await handler.Handle(Transition(wf.New.Id, wf.Approved.Id) with
        {
            Notifications = [Notification(type)], WorkflowId = wf.Workflow.Id
        }, CancellationToken.None);

        var n = db.Context.Set<TransitionNotification>().Single();
        Assert.Equal(type == ConstWorkflow.NotificationType.Push, n.IsSendCreator);
        Assert.Equal(type == ConstWorkflow.NotificationType.Push ? "Tiêu đề" : null, n.Title);
        Assert.Equal(type == ConstWorkflow.NotificationType.Zalo ? ConstWorkflow.ZaloDefault.Table : null, n.CrmTable);
        Assert.Equal(type == ConstWorkflow.NotificationType.Zalo ? "dbo" : null, n.CrmSchema);
        Assert.Equal(type == ConstWorkflow.NotificationType.Email ? 1 : 0, db.Context.Set<NotificationRecipient>().Count());
        Assert.Equal(type == ConstWorkflow.NotificationType.Email ? 1 : 0, db.Context.Set<NotificationAttachment>().Count());
    }

    [Fact]
    public async Task Transition_between_statuses_of_another_workflow_is_rejected()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf1 = WorkflowTestData.AddWorkflow(db, "WF1");
        var wf2 = WorkflowTestData.AddWorkflow(db, "WF2");
        var handler = new SaveTransitionCommandHandler(db.UnitOfWork, Metadata());

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            Transition(wf1.New.Id, wf2.Approved.Id) with { WorkflowId = wf1.Workflow.Id }, CancellationToken.None));
    }

    // ---------- Sơ đồ ----------

    [Fact]
    public async Task Diagram_groups_two_or_more_transitions_of_the_same_branch_into_a_rhombus()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        WorkflowTestData.AddTransition(db, wf, wf.New, wf.Approved, "Duyệt", "Kết quả");
        WorkflowTestData.AddTransition(db, wf, wf.New, wf.Rejected, "Từ chối", "Kết quả");
        WorkflowTestData.AddTransition(db, wf, wf.Approved, wf.Rejected, "Hủy", "Nhánh lẻ"); // nhánh có 1 bước → vẽ thẳng

        var d = await new GetWorkflowDiagramQueryHandler(db.UnitOfWork).Handle(new GetWorkflowDiagramQuery(wf.Workflow.Id), CancellationToken.None);

        var branch = Assert.Single(d.Branches);
        Assert.Equal($"{wf.New.Id}+KET_QUA", branch.Id);
        Assert.Equal((ConstWorkflow.DefaultBranchX, ConstWorkflow.DefaultBranchY), (branch.X, branch.Y));
        Assert.Single(d.Edges, e => e.IsBranchEntry && e.Source == wf.New.Id.ToString() && e.Target == branch.Id);
        Assert.Equal(2, d.Edges.Count(e => e.Source == branch.Id));
        Assert.Single(d.Edges, e => e.Label == "Hủy" && e.Source == wf.Approved.Id.ToString());
        // màu mặc định lấy theo process; chưa có vị trí → xếp hàng ngang
        var approved = d.Statuses.Single(s => s.Id == wf.Approved.Id);
        Assert.Equal(("#E3FCEF", 223, 100), (approved.BackgroundColor, approved.X, approved.Y));
    }

    [Fact]
    public async Task Moving_a_branch_updates_every_transition_of_the_group()
    {
        using var db = new TestDb();
        var wf = WorkflowTestData.AddWorkflow(db);
        var a = WorkflowTestData.AddTransition(db, wf, wf.New, wf.Approved, "Duyệt", "Kết quả");
        var b = WorkflowTestData.AddTransition(db, wf, wf.New, wf.Rejected, "Từ chối", "Kết quả");

        await new UpdateBranchPositionCommandHandler(db.UnitOfWork).Handle(
            new UpdateBranchPositionCommand(wf.New.Id, "KET_QUA", 500, 40) { WorkflowId = wf.Workflow.Id }, CancellationToken.None);

        Assert.All(db.Context.Set<StatusTransition>().Where(t => t.Id == a.Id || t.Id == b.Id), t => Assert.Equal((500, 40), (t.BranchPositionX, t.BranchPositionY)));
    }

    [Theory]
    [InlineData("Duyệt cấp 1", "DUYET_CAP_1")]
    [InlineData("  Đồng ý  ", "DONG_Y")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Branch_key_is_upper_unsigned_with_underscores(string? name, string? expected) =>
        Assert.Equal(expected, StatusTransition.ToBranchKey(name));
}
