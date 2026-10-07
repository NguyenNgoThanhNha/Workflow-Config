using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Kanbans.Commands.MoveKanbanStatus;

/// <summary>Kéo trạng thái sang cột khác trên bảng. ColumnId = null → bỏ khỏi cột (về "Chưa cấu hình").</summary>
public sealed record MoveKanbanStatusCommand(Guid StatusId, Guid? ColumnId) : IRequest
{
    [JsonIgnore]
    public Guid KanbanId { get; init; }
}

public sealed class MoveKanbanStatusCommandValidator : AbstractValidator<MoveKanbanStatusCommand>
{
    public MoveKanbanStatusCommandValidator() => RuleFor(x => x.StatusId).NotEmpty();
}

public sealed class MoveKanbanStatusCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork) : IRequestHandler<MoveKanbanStatusCommand>
{
    public async Task Handle(MoveKanbanStatusCommand request, CancellationToken ct)
    {
        if (!await unitOfWork.Repository<Kanban>().AnyAsync(k => k.Id == request.KanbanId, ct))
            throw new NotFoundException("Kanban", request.KanbanId);
        if (!await unitOfWork.Repository<WorkflowStatus>().AnyAsync(s => s.Id == request.StatusId, ct))
            throw new NotFoundException("Trạng thái", request.StatusId);
        if (request.ColumnId is { } columnId &&
            !await unitOfWork.Repository<KanbanColumn>().AnyAsync(c => c.Id == columnId && c.KanbanId == request.KanbanId, ct))
            throw new ValidationException(nameof(request.ColumnId), "Cột không thuộc bảng Kanban này.");

        var set = unitOfWork.Repository<KanbanStatusMapping>();
        var mapping = await set.FirstOrDefaultAsync(m => m.KanbanId == request.KanbanId && m.StatusId == request.StatusId, ct);
        if (request.ColumnId is null)
        {
            if (mapping is null) return;
            set.Remove(mapping);
        }
        else if (mapping is null)
        {
            set.Add(new KanbanStatusMapping { KanbanId = request.KanbanId, ColumnId = request.ColumnId.Value, StatusId = request.StatusId });
        }
        else
        {
            mapping.ColumnId = request.ColumnId.Value;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
