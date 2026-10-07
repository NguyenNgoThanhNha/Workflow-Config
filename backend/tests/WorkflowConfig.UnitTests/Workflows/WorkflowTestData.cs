using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.UnitTests.Workflows;

/// <summary>Dựng nhanh workflow mẫu (danh mục + 3 trạng thái) trong TestDb.</summary>
internal static class WorkflowTestData
{
    public static void SeedCatalogs(TestDb db)
    {
        db.Context.Set<WorkflowProcess>().AddRange(
            new WorkflowProcess { Code = ConstWorkflow.Process.Todo, Name = "Cần làm", BackgroundColor = "#DFE1E6", TextColor = "#42526E" },
            new WorkflowProcess { Code = ConstWorkflow.Process.Completed, Name = "Hoàn thành", BackgroundColor = "#E3FCEF", TextColor = "#006644" });
        db.Context.Set<TransitionUpdateMode>().AddRange(
            new TransitionUpdateMode { Code = ConstWorkflow.UpdateMode.NotConfig, Name = "Không cấu hình" },
            new TransitionUpdateMode { Code = ConstWorkflow.UpdateMode.Roles, Name = "Chọn nhóm" },
            new TransitionUpdateMode { Code = ConstWorkflow.UpdateMode.Department, Name = "Phòng ban" });
        db.Context.Set<WorkflowField>().AddRange(
            new WorkflowField { Code = "Summary", Name = "Tiêu đề", OrderIndex = 1 },
            new WorkflowField { Code = "Description", Name = "Mô tả", OrderIndex = 2 });
        db.Context.SaveChanges();
    }

    public sealed record Sample(Workflow Workflow, WorkflowStatus New, WorkflowStatus Approved, WorkflowStatus Rejected);

    public static Sample AddWorkflow(TestDb db, string code = "WF1")
    {
        var workflow = new Workflow { Code = code, Name = $"Workflow {code}", CategoryCode = "NV", CompanyCode = "1000", OrderIndex = 1 };
        workflow.RefreshSearchText();
        WorkflowStatus Status(string c, int order, string process, int? x = null) => new()
        {
            WorkflowId = workflow.Id, Code = c, Name = c, OrderIndex = order, ProcessCode = process, PositionX = x, PositionY = x
        };
        var s1 = Status("NEW", 1, ConstWorkflow.Process.Todo, 10);
        var s2 = Status("APPROVED", 2, ConstWorkflow.Process.Completed);
        var s3 = Status("REJECTED", 3, ConstWorkflow.Process.Completed, 300);
        db.Context.Set<Workflow>().Add(workflow);
        db.Context.Set<WorkflowStatus>().AddRange(s1, s2, s3);
        db.Context.Set<WorkflowFieldConfig>().Add(new WorkflowFieldConfig { WorkflowId = workflow.Id, FieldCode = "Summary", OrderIndex = 1 });
        db.Context.SaveChanges();
        return new Sample(workflow, s1, s2, s3);
    }

    public static StatusTransition AddTransition(TestDb db, Sample wf, WorkflowStatus from, WorkflowStatus to, string name, string? branch = null)
    {
        var t = new StatusTransition { WorkflowId = wf.Workflow.Id, FromStatusId = from.Id, ToStatusId = to.Id, Name = name };
        t.SetBranch(branch);
        db.Context.Set<StatusTransition>().Add(t);
        db.Context.SaveChanges();
        return t;
    }
}
