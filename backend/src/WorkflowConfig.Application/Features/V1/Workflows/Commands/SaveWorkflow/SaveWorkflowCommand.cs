using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Application.Features.V1.Workflows.Queries.GetWorkflowDetail;
using WorkflowConfig.Application.Features.V1.Workflows.Services;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveWorkflow;

/// <summary>Một dòng trạng thái trên form workflow. Id = null → trạng thái mới.</summary>
public sealed record WorkflowStatusInput(Guid? Id, string Code, string Name, int OrderIndex, string? Category, string ProcessCode);

/// <summary>Field được chọn hiển thị (chỉ gửi các field đã tick).</summary>
public sealed record WorkflowFieldConfigInput(
    string FieldCode,
    bool IsRequired,
    int? OrderIndex,
    string? Parameters,
    string? Note,
    string? NoteEn,
    bool HideWhenAdd,
    string? AddDefaultValue,
    bool HideWhenEdit,
    string? EditDefaultValue);

/// <summary>
/// Tạo (Id = null) hoặc sửa workflow cùng danh sách trạng thái và cấu hình thuộc tính
/// Ảnh đại diện upload riêng qua UploadWorkflowImageCommand.
/// </summary>
public sealed record SaveWorkflowCommand(
    string Code,
    string Name,
    string? CategoryCode,
    string? CompanyCode,
    int? OrderIndex,
    bool? IsActive,
    bool IsSummaryDisabled,
    IReadOnlyList<WorkflowStatusInput> Statuses,
    IReadOnlyList<WorkflowFieldConfigInput> Fields,
    byte[]? RowVersion) : IRequest<WorkflowDetailDto>
{
    [JsonIgnore]
    public Guid? Id { get; init; }
}

public sealed class SaveWorkflowCommandValidator : AbstractValidator<SaveWorkflowCommand>
{
    public SaveWorkflowCommandValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .MustAsync(async (cmd, code, ct) => !await unitOfWork.Repository<Workflow>()
                .AnyAsync(w => w.Code == code.Trim() && w.Id != (cmd.Id ?? Guid.Empty), ct))
            .WithMessage("Mã workflow đã tồn tại.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.CategoryCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(250);
        RuleFor(x => x.OrderIndex).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(x => x.RowVersion).NotEmpty().When(x => x.Id is not null).WithMessage("Thiếu rowVersion khi sửa.");

        RuleFor(x => x.Statuses).NotEmpty().WithMessage("Workflow phải có ít nhất một trạng thái.");
        RuleForEach(x => x.Statuses).ChildRules(s =>
        {
            s.RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
            s.RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
            s.RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
            s.RuleFor(x => x.Category).MaximumLength(100);
            s.RuleFor(x => x.ProcessCode).NotEmpty().WithMessage("Chọn nhóm xử lý (process) cho trạng thái.");
        });
        RuleFor(x => x.Statuses)
            .Must(list => list.Select(s => s.Code.Trim().ToUpperInvariant()).Distinct().Count() == list.Count)
            .When(x => x.Statuses is { Count: > 0 } && x.Statuses.All(s => !string.IsNullOrWhiteSpace(s.Code)))
            .WithMessage("Mã trạng thái bị trùng.");
        RuleFor(x => x.Statuses)
            .MustAsync(async (list, ct) =>
            {
                var codes = list.Select(s => s.ProcessCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
                return await unitOfWork.Repository<WorkflowProcess>().CountAsync(p => codes.Contains(p.Code), ct) == codes.Count;
            })
            .When(x => x.Statuses is { Count: > 0 })
            .WithMessage("Nhóm xử lý (process) không tồn tại.");

        RuleFor(x => x.Fields).NotNull();
        RuleFor(x => x.Fields)
            .Must(list => list.Select(f => f.FieldCode).Distinct().Count() == list.Count).WithMessage("Field bị trùng.")
            .MustAsync(async (list, ct) =>
            {
                var codes = list.Select(f => f.FieldCode).Distinct().ToList();
                return await unitOfWork.Repository<WorkflowField>().CountAsync(f => codes.Contains(f.Code), ct) == codes.Count;
            }).WithMessage("Field không tồn tại trong danh mục.")
            .When(x => x.Fields is not null);
    }
}

public sealed class SaveWorkflowCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<SaveWorkflowCommand, WorkflowDetailDto>
{
    public async Task<WorkflowDetailDto> Handle(SaveWorkflowCommand request, CancellationToken ct)
    {
        var workflows = unitOfWork.Repository<Workflow>();
        Workflow workflow;
        List<WorkflowStatus> statuses = [];
        List<WorkflowFieldConfig> fieldConfigs = [];

        if (request.Id is { } id)
        {
            workflow = await workflows.FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw new NotFoundException("Workflow", id);
            // Client gửi lại rowVersion đã đọc → EF kiểm tra khi UPDATE; lệch thì DbUpdateConcurrencyException → 409.
            unitOfWork.Repository<Workflow>().Entry(workflow).Property(w => w.RowVersion).OriginalValue = request.RowVersion!;
            statuses = await unitOfWork.Repository<WorkflowStatus>().Where(s => s.WorkflowId == id).ToListAsync(ct);
            fieldConfigs = await unitOfWork.Repository<WorkflowFieldConfig>().Where(f => f.WorkflowId == id).ToListAsync(ct);
            await EnsureRemovedStatusesAreFreeAsync(id, statuses, request.Statuses, ct);
        }
        else
        {
            workflow = new Workflow { Code = request.Code.Trim(), Name = request.Name.Trim() };
            workflows.Add(workflow);
        }

        workflow.Code = request.Code.Trim();
        workflow.Name = request.Name.Trim();
        workflow.CategoryCode = request.CategoryCode?.Trim();
        workflow.CompanyCode = request.CompanyCode?.Trim();
        workflow.OrderIndex = request.OrderIndex!.Value;
        workflow.IsActive = request.IsActive ?? true; // không gửi → mặc định "Đang sử dụng"
        workflow.IsSummaryDisabled = request.IsSummaryDisabled;
        workflow.RefreshSearchText();

        await SyncStatusesAsync(workflow.Id, statuses, request.Statuses, ct);
        SyncFieldConfigs(workflow.Id, fieldConfigs, request.Fields);

        // Sửa trạng thái / field cũng là sửa workflow: luôn UPDATE dòng cha để tăng RowVersion + ghi người sửa,
        // tránh hai người cùng sửa danh sách con mà không ai nhận 409.
        if (request.Id is not null) workflows.Entry(workflow).State = EntityState.Modified;

        await unitOfWork.SaveChangesAsync(ct);
        return await WorkflowDetailReader.ReadAsync(unitOfWork, workflow.Id, ct);
    }

    /// <summary>Trạng thái bị bỏ khỏi form mà còn bước chuyển → báo lỗi rõ ràng.</summary>
    private async Task EnsureRemovedStatusesAreFreeAsync(
        Guid workflowId, List<WorkflowStatus> existing, IReadOnlyList<WorkflowStatusInput> inputs, CancellationToken ct)
    {
        var keptIds = inputs.Where(i => i.Id is not null).Select(i => i.Id!.Value).ToHashSet();
        var removedIds = existing.Where(s => !keptIds.Contains(s.Id)).Select(s => s.Id).ToList();
        if (removedIds.Count == 0) return;

        var used = await unitOfWork.Repository<StatusTransition>()
            .Where(t => t.WorkflowId == workflowId && (removedIds.Contains(t.FromStatusId) || removedIds.Contains(t.ToStatusId)))
            .Select(t => t.FromStatusId)
            .Distinct()
            .ToListAsync(ct);
        if (used.Count == 0) return;

        var names = existing.Where(s => removedIds.Contains(s.Id)).Select(s => s.Name);
        throw new ConflictException($"Không thể xóa trạng thái đang có bước chuyển: {string.Join(", ", names)}. Xóa bước chuyển trên sơ đồ trước.");
    }

    private async Task SyncStatusesAsync(Guid workflowId, List<WorkflowStatus> existing, IReadOnlyList<WorkflowStatusInput> inputs, CancellationToken ct)
    {
        var set = unitOfWork.Repository<WorkflowStatus>();
        var keptIds = inputs.Where(i => i.Id is not null).Select(i => i.Id!.Value).ToHashSet();
        var removedIds = existing.Where(s => !keptIds.Contains(s.Id)).Select(s => s.Id).ToHashSet();
        if (removedIds.Count > 0)
        {
            // Quy tắc field theo trạng thái bị xóa cùng trạng thái.
            var rules = await unitOfWork.Repository<WorkflowStatusFieldRule>().Where(r => removedIds.Contains(r.StatusId)).ToListAsync(ct);
            unitOfWork.Repository<WorkflowStatusFieldRule>().RemoveRange(rules);
            unitOfWork.Repository<KanbanStatusMapping>().RemoveRange(
                await unitOfWork.Repository<KanbanStatusMapping>().Where(m => removedIds.Contains(m.StatusId)).ToListAsync(ct));
        }

        ChildSync.Apply(set, existing, inputs, s => s.Id, i => i.Id,
            (input, _) => new WorkflowStatus
            {
                WorkflowId = workflowId, Code = input.Code.Trim(), Name = input.Name.Trim(), OrderIndex = input.OrderIndex,
                Category = input.Category?.Trim(), ProcessCode = input.ProcessCode
            },
            (status, input, _) =>
            {
                status.Code = input.Code.Trim();
                status.Name = input.Name.Trim();
                status.OrderIndex = input.OrderIndex;
                status.Category = input.Category?.Trim();
                status.ProcessCode = input.ProcessCode;
                status.IsActive = true;
            });
    }

    private void SyncFieldConfigs(Guid workflowId, List<WorkflowFieldConfig> existing, IReadOnlyList<WorkflowFieldConfigInput> inputs)
    {
        var set = unitOfWork.Repository<WorkflowFieldConfig>();
        var byCode = existing.ToDictionary(f => f.FieldCode, StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            if (!byCode.Remove(input.FieldCode, out var config))
            {
                config = new WorkflowFieldConfig { WorkflowId = workflowId, FieldCode = input.FieldCode };
                set.Add(config);
            }

            config.IsRequired = input.IsRequired;
            config.OrderIndex = input.OrderIndex;
            config.Parameters = input.Parameters?.Trim();
            config.Note = input.Note?.Trim();
            config.NoteEn = input.NoteEn?.Trim();
            config.HideWhenAdd = input.HideWhenAdd;
            config.AddDefaultValue = input.AddDefaultValue?.Trim();
            config.HideWhenEdit = input.HideWhenEdit;
            config.EditDefaultValue = input.EditDefaultValue?.Trim();
        }

        set.RemoveRange(byCode.Values); // bỏ tick → xóa cấu hình
    }
}
