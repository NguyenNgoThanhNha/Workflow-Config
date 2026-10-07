using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.UpdateNodePosition;

/// <summary>Lưu vị trí ô trạng thái sau khi kéo thả trên sơ đồ.</summary>
public sealed record UpdateStatusPositionCommand(int X, int Y) : IRequest
{
    [JsonIgnore] public Guid WorkflowId { get; init; }
    [JsonIgnore] public Guid StatusId { get; init; }
}

public sealed class UpdateStatusPositionCommandValidator : AbstractValidator<UpdateStatusPositionCommand>
{
    public UpdateStatusPositionCommandValidator()
    {
        RuleFor(x => x.X).InclusiveBetween(-100_000, 100_000);
        RuleFor(x => x.Y).InclusiveBetween(-100_000, 100_000);
    }
}

public sealed class UpdateStatusPositionCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<UpdateStatusPositionCommand>
{
    public async Task Handle(UpdateStatusPositionCommand request, CancellationToken ct)
    {
        var status = await unitOfWork.Repository<WorkflowStatus>()
                         .FirstOrDefaultAsync(s => s.Id == request.StatusId && s.WorkflowId == request.WorkflowId, ct)
                     ?? throw new NotFoundException("Trạng thái", request.StatusId);
        status.PositionX = request.X;
        status.PositionY = request.Y;
        await unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>Lưu vị trí nút rẽ nhánh: cập nhật mọi bước chuyển cùng FromStatus + BranchKey.</summary>
public sealed record UpdateBranchPositionCommand(Guid FromStatusId, string BranchKey, int X, int Y) : IRequest
{
    [JsonIgnore] public Guid WorkflowId { get; init; }
}

public sealed class UpdateBranchPositionCommandValidator : AbstractValidator<UpdateBranchPositionCommand>
{
    public UpdateBranchPositionCommandValidator()
    {
        RuleFor(x => x.FromStatusId).NotEmpty();
        RuleFor(x => x.BranchKey).NotEmpty().MaximumLength(250);
        RuleFor(x => x.X).InclusiveBetween(-100_000, 100_000);
        RuleFor(x => x.Y).InclusiveBetween(-100_000, 100_000);
    }
}

public sealed class UpdateBranchPositionCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<UpdateBranchPositionCommand>
{
    public async Task Handle(UpdateBranchPositionCommand request, CancellationToken ct)
    {
        var transitions = await unitOfWork.Repository<StatusTransition>()
            .Where(t => t.WorkflowId == request.WorkflowId && t.FromStatusId == request.FromStatusId && t.BranchKey == request.BranchKey)
            .ToListAsync(ct);
        if (transitions.Count == 0) throw new NotFoundException("Nhánh", request.BranchKey);

        foreach (var t in transitions)
        {
            t.BranchPositionX = request.X;
            t.BranchPositionY = request.Y;
        }
        await unitOfWork.SaveChangesAsync(ct);
    }
}
