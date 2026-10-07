using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowImage;

/// <summary>Ảnh đại diện workflow (FE tải bằng access token rồi hiển thị qua object URL).</summary>
public sealed record GetWorkflowImageQuery(Guid Id) : IRequest<WorkflowImageDto>;

public sealed class GetWorkflowImageQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, IFileStorage storage)
    : IRequestHandler<GetWorkflowImageQuery, WorkflowImageDto>
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".webp"] = "image/webp"
    };

    public async Task<WorkflowImageDto> Handle(GetWorkflowImageQuery request, CancellationToken ct)
    {
        var path = await unitOfWork.Repository<Workflow>().AsNoTracking()
            .Where(w => w.Id == request.Id)
            .Select(w => w.ImagePath)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Ảnh workflow", request.Id);

        Stream content;
        try
        {
            content = await storage.OpenReadAsync(path, ct);
        }
        catch (FileNotFoundException)
        {
            // Có đường dẫn trong DB nhưng file đã mất trên storage → 404 thay vì 500.
            throw new NotFoundException("Ảnh workflow", request.Id);
        }

        return new WorkflowImageDto(content, ContentTypes.GetValueOrDefault(Path.GetExtension(path), "application/octet-stream"));
    }
}
