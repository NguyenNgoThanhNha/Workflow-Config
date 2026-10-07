using MediatR;
using Microsoft.AspNetCore.Mvc;
using WorkflowConfig.Api.Authorization;
using WorkflowConfig.Application.Common.Models;
using WorkflowConfig.Application.Features.V1.Kanbans.Commands.DeleteKanban;
using WorkflowConfig.Application.Features.V1.Kanbans.Commands.MoveKanbanStatus;
using WorkflowConfig.Application.Features.V1.Kanbans.Commands.SaveKanban;
using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanban;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries.GetKanbanBoard;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries.SearchKanbans;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Enums;

namespace WorkflowConfig.Api.Controllers.V1;

/// <summary>Bảng Kanban: danh mục cột và cách xếp trạng thái workflow vào cột.</summary>
[Route("api/v1/kanbans")]
public sealed class KanbansController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    [HasPermission(ConstActivity.Kanban, ActivityType.Read)]
    public async Task<ActionResult<PagedResult<KanbanListItemDto>>> Search([FromQuery] SearchKanbansQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(ConstActivity.Kanban, ActivityType.Read)]
    public async Task<ActionResult<KanbanDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetKanbanQuery(id), ct));

    [HttpPost]
    [HasPermission(ConstActivity.Kanban, ActivityType.Create)]
    public async Task<ActionResult<KanbanDetailDto>> Create(SaveKanbanCommand command, CancellationToken ct)
    {
        var kanban = await Mediator.Send(command with { Id = null }, ct);
        return CreatedAtAction(nameof(Get), new { id = kanban.Id }, kanban);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(ConstActivity.Kanban, ActivityType.Update)]
    public async Task<ActionResult<KanbanDetailDto>> Update(Guid id, SaveKanbanCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(ConstActivity.Kanban, ActivityType.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteKanbanCommand(id), ct);
        return NoContent();
    }

    /// <summary>Cột + thẻ trạng thái; lọc theo workflow bằng ?workflowId=.</summary>
    [HttpGet("{id:guid}/board")]
    [HasPermission(ConstActivity.Kanban, ActivityType.Read)]
    public async Task<ActionResult<KanbanBoardDto>> Board(Guid id, [FromQuery] Guid? workflowId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetKanbanBoardQuery(id, workflowId), ct));

    [HttpPut("{id:guid}/mappings")]
    [HasPermission(ConstActivity.Kanban, ActivityType.Update)]
    public async Task<IActionResult> Move(Guid id, MoveKanbanStatusCommand command, CancellationToken ct)
    {
        await Mediator.Send(command with { KanbanId = id }, ct);
        return NoContent();
    }
}
