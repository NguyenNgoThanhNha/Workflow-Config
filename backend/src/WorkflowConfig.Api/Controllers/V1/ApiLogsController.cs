using MediatR;
using Microsoft.AspNetCore.Mvc;
using WorkflowConfig.Api.Authorization;
using WorkflowConfig.Application.Common.Models;
using WorkflowConfig.Application.Features.V1.ApiLogs.DTOs;
using WorkflowConfig.Application.Features.V1.ApiLogs.Queries;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Enums;

namespace WorkflowConfig.Api.Controllers.V1;

/// <summary>Tra cứu log request/response API để debug (chuẩn BE §9.2 · RULES.md §8).</summary>
[Route("api/v1/api-logs")]
[HasPermission(ConstActivity.ApiLog, ActivityType.Read)]
public sealed class ApiLogsController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApiLogListItemDto>>> Search([FromQuery] SearchApiLogsQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiLogDetailDto>> Get(long id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetApiLogDetailQuery(id), ct));
}
