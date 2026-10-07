using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Kanbans.DTOs;
using WorkflowConfig.Application.Features.V1.Kanbans.Queries;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Kanbans.Commands.DeleteKanban;

/// <summary>Xóa bảng Kanban cùng cột và cách xếp trạng thái (không ảnh hưởng workflow).</summary>
public sealed record DeleteKanbanCommand(Guid Id) : IRequest;

public sealed class DeleteKanbanCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork) : IRequestHandler<DeleteKanbanCommand>
{
    public async Task Handle(DeleteKanbanCommand request, CancellationToken ct)
    {
        var kanban = await unitOfWork.Repository<Kanban>().FirstOrDefaultAsync(k => k.Id == request.Id, ct)
                     ?? throw new NotFoundException("Kanban", request.Id);
        unitOfWork.Repository<KanbanStatusMapping>().RemoveRange(
            await unitOfWork.Repository<KanbanStatusMapping>().Where(m => m.KanbanId == kanban.Id).ToListAsync(ct));
        unitOfWork.Repository<KanbanColumn>().RemoveRange(
            await unitOfWork.Repository<KanbanColumn>().Where(c => c.KanbanId == kanban.Id).ToListAsync(ct));
        unitOfWork.Repository<Kanban>().Remove(kanban);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
