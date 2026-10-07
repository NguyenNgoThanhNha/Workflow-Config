using WorkflowConfig.Application.Common.Exceptions;
using WorkflowConfig.Application.Features.V1.Kanbans.Commands.MoveKanbanStatus;
using WorkflowConfig.Application.Features.V1.Kanbans.Commands.SaveKanban;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanbanBoard;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.DeleteStatus;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.UnitTests.Workflows;

public class KanbanHandlerTests
{
    private static async Task<(Guid KanbanId, Guid Todo, Guid Done)> CreateKanbanAsync(TestDb db, string code = "KB1")
    {
        var created = await new SaveKanbanCommandHandler(db.UnitOfWork).Handle(new SaveKanbanCommand(code, "Bảng", 1, null,
            [new KanbanColumnInput(null, "Cần làm", 1, null, "#42526e"), new KanbanColumnInput(null, "Xong", 2, "ghi chú", null)], null),
            CancellationToken.None);
        return (created.Id, created.Columns[0].Id, created.Columns[1].Id);
    }

    private static Task Move(TestDb db, Guid kanbanId, Guid statusId, Guid? columnId) =>
        new MoveKanbanStatusCommandHandler(db.UnitOfWork)
            .Handle(new MoveKanbanStatusCommand(statusId, columnId) { KanbanId = kanbanId }, CancellationToken.None);

    [Fact]
    public async Task Create_saves_columns_in_order_with_normalized_color()
    {
        using var db = new TestDb();
        var (id, _, _) = await CreateKanbanAsync(db);

        var kanban = db.Context.Set<Kanban>().Single(k => k.Id == id);
        Assert.True(kanban.IsActive);
        var columns = db.Context.Set<KanbanColumn>().OrderBy(c => c.OrderIndex).ToList();
        Assert.Equal(["Cần làm", "Xong"], columns.Select(c => c.Name));
        Assert.Equal("#42526E", columns[0].Color);
    }

    [Fact]
    public async Task Move_maps_remaps_and_unmaps_a_status()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var (id, todo, done) = await CreateKanbanAsync(db);

        await Move(db, id, wf.New.Id, todo);
        await Move(db, id, wf.New.Id, done); // kéo sang cột khác: vẫn một dòng
        var mapping = Assert.Single(db.Context.Set<KanbanStatusMapping>());
        Assert.Equal(done, mapping.ColumnId);

        await Move(db, id, wf.New.Id, null);
        Assert.Empty(db.Context.Set<KanbanStatusMapping>());
    }

    [Fact]
    public async Task Move_rejects_a_column_of_another_board()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var (id, _, _) = await CreateKanbanAsync(db, "KB1");
        var (_, otherColumn, _) = await CreateKanbanAsync(db, "KB2");

        await Assert.ThrowsAsync<ValidationException>(() => Move(db, id, wf.New.Id, otherColumn));
    }

    [Fact]
    public async Task Board_lists_every_status_and_unmapped_ones_have_no_column()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf1 = WorkflowTestData.AddWorkflow(db, "WF1");
        var wf2 = WorkflowTestData.AddWorkflow(db, "WF2");
        var (id, todo, _) = await CreateKanbanAsync(db);
        await Move(db, id, wf1.New.Id, todo);
        var handler = new GetKanbanBoardQueryHandler(db.UnitOfWork);

        var all = await handler.Handle(new GetKanbanBoardQuery(id, null), CancellationToken.None);
        Assert.Equal(6, all.Cards.Count);
        Assert.Equal(2, all.Workflows.Count);
        Assert.Equal(todo, all.Cards.Single(c => c.StatusId == wf1.New.Id).ColumnId);
        Assert.Equal(5, all.Cards.Count(c => c.ColumnId is null));
        Assert.Equal("#DFE1E6", all.Cards.Single(c => c.StatusId == wf1.New.Id).BackgroundColor); // màu theo process

        var filtered = await handler.Handle(new GetKanbanBoardQuery(id, wf2.Workflow.Id), CancellationToken.None);
        Assert.All(filtered.Cards, c => Assert.Equal(wf2.Workflow.Id, c.WorkflowId));
    }

    [Fact]
    public async Task Removing_a_column_or_deleting_a_status_drops_its_mappings()
    {
        using var db = new TestDb();
        WorkflowTestData.SeedCatalogs(db);
        var wf = WorkflowTestData.AddWorkflow(db);
        var (id, todo, done) = await CreateKanbanAsync(db);
        await Move(db, id, wf.New.Id, todo);
        await Move(db, id, wf.Rejected.Id, done);

        // bỏ cột "Cần làm"
        var kanban = db.Context.Set<Kanban>().Single();
        await new SaveKanbanCommandHandler(db.UnitOfWork).Handle(new SaveKanbanCommand("KB1", "Bảng", 1, true,
            [new KanbanColumnInput(done, "Xong", 1, null, null)], kanban.RowVersion) { Id = id }, CancellationToken.None);
        Assert.Equal(wf.Rejected.Id, Assert.Single(db.Context.Set<KanbanStatusMapping>()).StatusId);

        // xóa trạng thái REJECTED trên sơ đồ
        await new DeleteWorkflowStatusCommandHandler(db.UnitOfWork)
            .Handle(new DeleteWorkflowStatusCommand(wf.Workflow.Id, wf.Rejected.Id), CancellationToken.None);
        Assert.Empty(db.Context.Set<KanbanStatusMapping>());
    }
}
