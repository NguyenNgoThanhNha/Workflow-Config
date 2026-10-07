using FluentValidation;
using Microsoft.Extensions.Options;
using WorkflowConfig.Application.Features.V1.Workflows.Services;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.UploadWorkflowImage;

/// <summary>Đổi ảnh đại diện workflow.</summary>
public sealed record UploadWorkflowImageCommand(Guid Id, Stream Content, string FileName, long Length) : IRequest;

public sealed class UploadWorkflowImageCommandValidator : AbstractValidator<UploadWorkflowImageCommand>
{
    public UploadWorkflowImageCommandValidator(IOptions<WorkflowOptions> options)
    {
        var o = options.Value;
        RuleFor(x => x.Length).GreaterThan(0).WithMessage("File rỗng.")
            .LessThanOrEqualTo(o.ImageMaxBytes).WithMessage($"Ảnh tối đa {o.ImageMaxBytes / 1024 / 1024} MB.");
        RuleFor(x => x.FileName)
            .Must(name => o.ImageExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Chỉ nhận ảnh {string.Join(", ", o.ImageExtensions)}.");
    }
}

public sealed class UploadWorkflowImageCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, IFileStorage storage)
    : IRequestHandler<UploadWorkflowImageCommand>
{
    public async Task Handle(UploadWorkflowImageCommand request, CancellationToken ct)
    {
        var workflow = await unitOfWork.Repository<Workflow>().FirstOrDefaultAsync(w => w.Id == request.Id, ct)
                       ?? throw new NotFoundException("Workflow", request.Id);

        // Không xóa file cũ: workflow copy có thể đang dùng chung đường dẫn ảnh.
        workflow.ImagePath = await storage.SaveAsync(request.Content, request.FileName, ConstWorkflow.ImageFolder, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
