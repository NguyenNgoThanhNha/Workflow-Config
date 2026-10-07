using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Sys;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowDiagram;

/// <summary>
/// Dữ liệu sơ đồ workflow:
/// - Ô trạng thái: màu riêng nếu đã đặt, không thì màu của nhóm xử lý.
/// - Bước chuyển cùng FromStatus và cùng nhánh (≥ 2 bước) vẽ qua một nút hình thoi.
/// - Nhãn mũi tên: tên bước chuyển + "(role)" khi giao theo nhóm, "(giá trị)" khi giao theo phòng ban.
/// </summary>
public sealed record GetWorkflowDiagramQuery(Guid WorkflowId) : IRequest<WorkflowDiagramDto>;

public sealed class GetWorkflowDiagramQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetWorkflowDiagramQuery, WorkflowDiagramDto>
{
    private const string DefaultBackground = "#FFFFFF";
    private const string DefaultText = "#000000";

    public async Task<WorkflowDiagramDto> Handle(GetWorkflowDiagramQuery request, CancellationToken ct)
    {
        var workflow = await unitOfWork.Repository<Workflow>().AsNoTracking()
            .Where(w => w.Id == request.WorkflowId)
            .Select(w => new { w.Id, w.Code, w.Name })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Workflow", request.WorkflowId);

        var statuses = await (
                from s in unitOfWork.Repository<WorkflowStatus>().AsNoTracking()
                where s.WorkflowId == request.WorkflowId
                join p in unitOfWork.Repository<WorkflowProcess>().AsNoTracking() on s.ProcessCode equals p.Code into pj
                from p in pj.DefaultIfEmpty()
                orderby s.OrderIndex
                select new
                {
                    s.Id, s.Code, s.Name, s.ProcessCode, s.PositionX, s.PositionY, s.TextColor, s.BackgroundColor,
                    ProcessBackground = p != null ? p.BackgroundColor : null,
                    ProcessText = p != null ? p.TextColor : null
                })
            .ToListAsync(ct);

        // Chưa từng kéo thả → xếp hàng ngang (x = 23 + 200*i, y = 100).
        var nodes = statuses.Select((s, i) =>
        {
            var useOwnColor = !string.IsNullOrEmpty(s.TextColor);
            return new DiagramStatusNodeDto(
                s.Id, s.Code, s.Name, s.ProcessCode, s.PositionX ?? 23 + 200 * i, s.PositionY ?? 100,
                (useOwnColor ? s.BackgroundColor : s.ProcessBackground) ?? DefaultBackground,
                (useOwnColor ? s.TextColor : s.ProcessText) ?? DefaultText);
        }).ToList();

        var transitions = await unitOfWork.Repository<StatusTransition>().AsNoTracking()
            .Where(t => t.WorkflowId == request.WorkflowId)
            .OrderBy(t => t.OrderIndex).ThenBy(t => t.CreatedDate)
            .Select(t => new
            {
                t.Id, t.FromStatusId, t.ToStatusId, t.Name, t.BranchName, t.BranchKey, t.BranchPositionX, t.BranchPositionY,
                t.SourceAnchor, t.TargetAnchor, t.Color, t.AssigneeUpdateMode, t.AssigneeRoleId, t.AssigneeValue
            })
            .ToListAsync(ct);

        var roleIds = transitions.Where(t => t.AssigneeRoleId != null).Select(t => t.AssigneeRoleId!.Value).Distinct().ToList();
        var roleNames = await unitOfWork.Repository<SysRole>().AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, ct);

        string Label(string name, string? mode, Guid? roleId, string? value) => mode switch
        {
            ConstWorkflow.UpdateMode.Roles when roleId is { } id && roleNames.TryGetValue(id, out var role) => $"{name} ({role})",
            ConstWorkflow.UpdateMode.Department when !string.IsNullOrWhiteSpace(value) => $"{name} ({value})",
            _ => name
        };

        var branchGroups = transitions
            .Where(t => t.BranchKey != null)
            .GroupBy(t => (t.FromStatusId, t.BranchKey))
            .Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.ToList());

        var branches = branchGroups.Select(g =>
        {
            var first = g.Value[0];
            return new DiagramBranchNodeDto(
                BranchNodeId(first.FromStatusId, first.BranchKey!), first.FromStatusId, first.BranchKey!, first.BranchName ?? first.BranchKey!,
                first.BranchPositionX ?? ConstWorkflow.DefaultBranchX, first.BranchPositionY ?? ConstWorkflow.DefaultBranchY);
        }).ToList();

        var edges = new List<DiagramEdgeDto>();
        foreach (var group in branchGroups.Values)
        {
            var first = group[0];
            var branchId = BranchNodeId(first.FromStatusId, first.BranchKey!);
            edges.Add(new DiagramEdgeDto($"entry:{branchId}", first.Id, first.FromStatusId.ToString(), branchId, null, null, null, null, true));
        }

        foreach (var t in transitions)
        {
            var label = Label(t.Name, t.AssigneeUpdateMode, t.AssigneeRoleId, t.AssigneeValue);
            var inBranch = t.BranchKey != null && branchGroups.ContainsKey((t.FromStatusId, t.BranchKey));
            var source = inBranch ? BranchNodeId(t.FromStatusId, t.BranchKey!) : t.FromStatusId.ToString();
            edges.Add(new DiagramEdgeDto(t.Id.ToString(), t.Id, source, t.ToStatusId.ToString(),
                inBranch ? null : t.SourceAnchor, t.TargetAnchor, label, t.Color, false));
        }

        return new WorkflowDiagramDto(workflow.Id, workflow.Code, workflow.Name, nodes, branches, edges);
    }

    public static string BranchNodeId(Guid fromStatusId, string branchKey) => $"{fromStatusId}{ConstWorkflow.BranchNodeSeparator}{branchKey}";
}
