using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveStatus;

public sealed record StatusFieldRuleInput(
    string FieldCode,
    bool DisableForCreator,
    bool RequiredForCreator,
    bool DisableForAssignee,
    bool RequiredForAssignee,
    bool DisableForReporter,
    bool RequiredForReporter);

/// <summary>Thêm (StatusId = null) hoặc sửa trạng thái từ sơ đồ (UpdateTaskStatus cũ), kèm quy tắc field theo vai trò.</summary>
public sealed record SaveWorkflowStatusCommand(
    string Code,
    string Name,
    int? OrderIndex,
    string? Category,
    string ProcessCode,
    string? TextColor,
    string? BackgroundColor,
    string? CustomColor,
    bool AutoUpdateEndDate,
    bool IsPushNotification,
    bool IsSendCreator,
    bool IsSendAssignee,
    bool IsSendMonitor,
    string? NotificationTitle,
    string? NotificationMessage,
    IReadOnlyList<StatusFieldRuleInput>? FieldRules) : IRequest<Guid>
{
    [JsonIgnore] public Guid WorkflowId { get; init; }
    [JsonIgnore] public Guid? StatusId { get; init; }
}

public sealed class SaveWorkflowStatusCommandValidator : AbstractValidator<SaveWorkflowStatusCommand>
{
    private const string ColorPattern = "^#[0-9A-Fa-f]{6}$";

    public SaveWorkflowStatusCommandValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .MustAsync(async (cmd, code, ct) => !await unitOfWork.Repository<WorkflowStatus>().AnyAsync(s =>
                s.WorkflowId == cmd.WorkflowId && s.Code == code.Trim() && s.Id != (cmd.StatusId ?? Guid.Empty), ct))
            .WithMessage("Mã trạng thái đã tồn tại trong workflow.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.OrderIndex).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.ProcessCode).NotEmpty()
            .MustAsync((code, ct) => unitOfWork.Repository<WorkflowProcess>().AnyAsync(p => p.Code == code, ct))
            .WithMessage("Nhóm xử lý (process) không tồn tại.");
        RuleFor(x => x.TextColor).Matches(ColorPattern).When(x => !string.IsNullOrEmpty(x.TextColor));
        RuleFor(x => x.BackgroundColor).Matches(ColorPattern).When(x => !string.IsNullOrEmpty(x.BackgroundColor));
        RuleFor(x => x.CustomColor).Matches(ColorPattern).When(x => !string.IsNullOrEmpty(x.CustomColor));
        RuleFor(x => x.NotificationTitle).MaximumLength(250);
        RuleFor(x => x.NotificationMessage).MaximumLength(1000);
        RuleForEach(x => x.FieldRules).ChildRules(r => r.RuleFor(x => x.FieldCode).NotEmpty());
    }
}

public sealed class SaveWorkflowStatusCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<SaveWorkflowStatusCommand, Guid>
{
    public async Task<Guid> Handle(SaveWorkflowStatusCommand request, CancellationToken ct)
    {
        if (!await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Id == request.WorkflowId, ct))
            throw new NotFoundException("Workflow", request.WorkflowId);

        WorkflowStatus status;
        if (request.StatusId is { } id)
        {
            status = await unitOfWork.Repository<WorkflowStatus>().FirstOrDefaultAsync(s => s.Id == id && s.WorkflowId == request.WorkflowId, ct)
                     ?? throw new NotFoundException("Trạng thái", id);
        }
        else
        {
            status = new WorkflowStatus { WorkflowId = request.WorkflowId, Code = request.Code, Name = request.Name, ProcessCode = request.ProcessCode };
            unitOfWork.Repository<WorkflowStatus>().Add(status);
        }

        status.Code = request.Code.Trim();
        status.Name = request.Name.Trim();
        status.OrderIndex = request.OrderIndex!.Value;
        status.Category = request.Category?.Trim();
        status.ProcessCode = request.ProcessCode;
        status.TextColor = NullIfEmpty(request.TextColor);
        status.BackgroundColor = NullIfEmpty(request.BackgroundColor);
        status.CustomColor = NullIfEmpty(request.CustomColor);
        status.AutoUpdateEndDate = request.AutoUpdateEndDate;
        status.IsPushNotification = request.IsPushNotification;
        // Tắt push → bỏ luôn người nhận/nội dung để không còn cấu hình "mồ côi".
        status.IsSendCreator = request.IsPushNotification && request.IsSendCreator;
        status.IsSendAssignee = request.IsPushNotification && request.IsSendAssignee;
        status.IsSendMonitor = request.IsPushNotification && request.IsSendMonitor;
        status.NotificationTitle = request.IsPushNotification ? request.NotificationTitle?.Trim() : null;
        status.NotificationMessage = request.IsPushNotification ? request.NotificationMessage?.Trim() : null;

        if (request.FieldRules is not null) await SyncFieldRulesAsync(status, request.FieldRules, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return status.Id;
    }

    /// <summary>Như bản cũ: xóa cấu hình cũ, chỉ giữ field có ít nhất một cờ được tick.</summary>
    private async Task SyncFieldRulesAsync(WorkflowStatus status, IReadOnlyList<StatusFieldRuleInput> inputs, CancellationToken ct)
    {
        var set = unitOfWork.Repository<WorkflowStatusFieldRule>();
        var existing = await set.Where(r => r.StatusId == status.Id).ToDictionaryAsync(r => r.FieldCode, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var input in inputs)
        {
            var rule = existing.Remove(input.FieldCode, out var found)
                ? found
                : new WorkflowStatusFieldRule { WorkflowId = status.WorkflowId, StatusId = status.Id, FieldCode = input.FieldCode };
            rule.DisableForCreator = input.DisableForCreator;
            rule.RequiredForCreator = input.RequiredForCreator;
            rule.DisableForAssignee = input.DisableForAssignee;
            rule.RequiredForAssignee = input.RequiredForAssignee;
            rule.DisableForReporter = input.DisableForReporter;
            rule.RequiredForReporter = input.RequiredForReporter;

            if (!rule.HasAnyFlag)
            {
                if (found is not null) set.Remove(rule);
                continue;
            }
            if (found is null) set.Add(rule);
        }

        set.RemoveRange(existing.Values);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
