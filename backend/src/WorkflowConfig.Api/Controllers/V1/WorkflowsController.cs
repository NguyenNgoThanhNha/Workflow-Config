using MediatR;
using Microsoft.AspNetCore.Mvc;
using WorkflowConfig.Api.Authorization;
using WorkflowConfig.Application.Common.Models;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.CopyWorkflow;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.DeleteStatus;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.DeleteTransition;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveStatus;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveTransition;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveWorkflow;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.UpdateNodePosition;
using WorkflowConfig.Application.Features.V1.Workflows.Commands.UploadWorkflowImage;
using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetStatusForm;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetTransition;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetTransitionTable;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowDetail;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowDiagram;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowImage;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowLookups;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.SearchWorkflows;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Enums;

namespace WorkflowConfig.Api.Controllers.V1;

/// <summary>Cấu hình quy trình.</summary>
[Route("api/v1/workflows")]
public sealed class WorkflowsController(ISender mediator) : ApiControllerBase(mediator)
{
    // ---------- Workflow ----------

    [HttpGet]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<PagedResult<WorkflowListItemDto>>> Search([FromQuery] SearchWorkflowsQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));

    [HttpGet("lookups")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<WorkflowLookupsDto>> Lookups(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetWorkflowLookupsQuery(), ct));

    /// <summary>Danh mục field kèm cấu hình rỗng — cho form tạo mới.</summary>
    [HttpGet("fields")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<IReadOnlyList<WorkflowFieldConfigDto>>> Fields(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetWorkflowFieldTemplateQuery(), ct));

    [HttpGet("crm-tables")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<IReadOnlyList<string>>> CrmTables(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCrmTablesQuery(), ct));

    [HttpGet("crm-tables/{table}/columns")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<IReadOnlyList<string>>> CrmColumns(string table, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCrmColumnsQuery(table), ct));

    [HttpGet("{id:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<WorkflowDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetWorkflowDetailQuery(id), ct));

    [HttpPost]
    [HasPermission(ConstActivity.Workflow, ActivityType.Create)]
    public async Task<ActionResult<WorkflowDetailDto>> Create(SaveWorkflowCommand command, CancellationToken ct)
    {
        var workflow = await Mediator.Send(command with { Id = null }, ct);
        return CreatedAtAction(nameof(Get), new { id = workflow.Id }, workflow);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<ActionResult<WorkflowDetailDto>> Update(Guid id, SaveWorkflowCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { Id = id }, ct));

    [HttpPost("{id:guid}/copy")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Create)]
    public async Task<ActionResult<WorkflowCopiedDto>> Copy(Guid id, CopyWorkflowCommand command, CancellationToken ct)
    {
        var copied = await Mediator.Send(command with { SourceId = id }, ct);
        return CreatedAtAction(nameof(Get), new { id = copied.Id }, copied);
    }

    [HttpGet("{id:guid}/image")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<IActionResult> GetImage(Guid id, CancellationToken ct)
    {
        var image = await Mediator.Send(new GetWorkflowImageQuery(id), ct);
        return File(image.Content, image.ContentType);
    }

    [HttpPut("{id:guid}/image")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        await Mediator.Send(new UploadWorkflowImageCommand(id, stream, file.FileName, file.Length), ct);
        return NoContent();
    }

    // ---------- Sơ đồ ----------

    [HttpGet("{id:guid}/diagram")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<WorkflowDiagramDto>> Diagram(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetWorkflowDiagramQuery(id), ct));

    [HttpGet("{id:guid}/transition-table")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<IReadOnlyList<TransitionTableRowDto>>> TransitionTable(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetTransitionTableQuery(id), ct));

    [HttpPut("{id:guid}/statuses/{statusId:guid}/position")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<IActionResult> MoveStatus(Guid id, Guid statusId, UpdateStatusPositionCommand command, CancellationToken ct)
    {
        await Mediator.Send(command with { WorkflowId = id, StatusId = statusId }, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/branches/position")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<IActionResult> MoveBranch(Guid id, UpdateBranchPositionCommand command, CancellationToken ct)
    {
        await Mediator.Send(command with { WorkflowId = id }, ct);
        return NoContent();
    }

    // ---------- Trạng thái ----------

    /// <summary>Form trạng thái: không truyền statusId → dữ liệu mặc định cho thêm mới.</summary>
    [HttpGet("{id:guid}/statuses/form")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<WorkflowStatusFormDto>> StatusForm(Guid id, [FromQuery] Guid? statusId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetStatusFormQuery(id, statusId), ct));

    [HttpPost("{id:guid}/statuses")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<ActionResult<Guid>> CreateStatus(Guid id, SaveWorkflowStatusCommand command, CancellationToken ct)
    {
        var statusId = await Mediator.Send(command with { WorkflowId = id, StatusId = null }, ct);
        return CreatedAtAction(nameof(StatusForm), new { id, statusId }, statusId);
    }

    [HttpPut("{id:guid}/statuses/{statusId:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<IActionResult> UpdateStatus(Guid id, Guid statusId, SaveWorkflowStatusCommand command, CancellationToken ct)
    {
        await Mediator.Send(command with { WorkflowId = id, StatusId = statusId }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/statuses/{statusId:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Delete)]
    public async Task<IActionResult> DeleteStatus(Guid id, Guid statusId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteWorkflowStatusCommand(id, statusId), ct);
        return NoContent();
    }

    // ---------- Bước chuyển ----------

    [HttpGet("{id:guid}/transitions/{transitionId:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Read)]
    public async Task<ActionResult<TransitionDetailDto>> GetTransition(Guid id, Guid transitionId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetTransitionQuery(id, transitionId), ct));

    [HttpPost("{id:guid}/transitions")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<ActionResult<Guid>> CreateTransition(Guid id, SaveTransitionCommand command, CancellationToken ct)
    {
        var transitionId = await Mediator.Send(command with { WorkflowId = id, TransitionId = null }, ct);
        return CreatedAtAction(nameof(GetTransition), new { id, transitionId }, transitionId);
    }

    [HttpPut("{id:guid}/transitions/{transitionId:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Update)]
    public async Task<IActionResult> UpdateTransition(Guid id, Guid transitionId, SaveTransitionCommand command, CancellationToken ct)
    {
        await Mediator.Send(command with { WorkflowId = id, TransitionId = transitionId }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/transitions/{transitionId:guid}")]
    [HasPermission(ConstActivity.Workflow, ActivityType.Delete)]
    public async Task<IActionResult> DeleteTransition(Guid id, Guid transitionId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteTransitionCommand(id, transitionId), ct);
        return NoContent();
    }
}
