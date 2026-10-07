using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Workflows.Services;
using WorkflowConfig.Domain.Entities.Sys;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.SaveTransition;

public sealed record AutoConditionInput(
    Guid? Id,
    string? Connector,
    string? ConditionType,
    string? Field,
    string? ComparisonType,
    string? ValueType,
    string? Value,
    string? SqlText);

public sealed record NotificationRecipientInput(Guid? Id, string? Mode, string? ConfigValue);

public sealed record NotificationAttachmentInput(Guid? Id, string Attachment);

/// <summary>
/// Một cấu hình thông báo. Zalo: UseDefaultZaloData = true → nguồn TaskModel.Text7 như bản cũ ("Mặc định"),
/// false → chọn bảng/cột (CrmTable/CrmField). Cc/Bcc/Attachments chỉ dùng cho EMAIL.
/// </summary>
public sealed record TransitionNotificationInput(
    Guid? Id,
    string Type,
    string? Mode,
    string? ConfigValue,
    Guid? TemplateId,
    string? ZnsTemplateId,
    bool UseDefaultZaloData,
    string? CrmTable,
    string? CrmField,
    bool IsSendCreator,
    bool IsSendAssignee,
    bool IsSendMonitor,
    string? Title,
    string? Message,
    IReadOnlyList<NotificationRecipientInput>? Cc,
    IReadOnlyList<NotificationRecipientInput>? Bcc,
    IReadOnlyList<NotificationAttachmentInput>? Attachments);

/// <summary>
/// Tạo (TransitionId = null) hoặc sửa bước chuyển cùng điều kiện tự động và cấu hình thông báo
/// (SaveTest + Save cũ). Danh sách con đồng bộ theo Id: dòng mất khỏi form thì bị xóa.
/// </summary>
public sealed record SaveTransitionCommand(
    string Name,
    string? Description,
    int? OrderIndex,
    string? BranchName,
    Guid FromStatusId,
    Guid ToStatusId,
    string? SourceAnchor,
    string? TargetAnchor,
    string? Color,
    string? TextColor,
    Guid? PermissionRoleId,
    bool IsCreatorAllowed,
    bool IsAssigneeAllowed,
    bool IsReporterAllowed,
    bool IsCommentShown,
    bool IsCommentRequired,
    bool IsDropdownShown,
    bool IsDropdownRequired,
    string? DropdownValueType,
    bool IsAutomatic,
    string? AssigneeUpdateMode,
    Guid? AssigneeRoleId,
    string? AssigneeValue,
    string? ReporterUpdateMode,
    Guid? ReporterRoleId,
    string? ReporterValue,
    string? SignatureType,
    string? SignerType,
    IReadOnlyList<AutoConditionInput>? Conditions,
    IReadOnlyList<TransitionNotificationInput>? Notifications) : IRequest<Guid>
{
    [JsonIgnore] public Guid WorkflowId { get; init; }
    [JsonIgnore] public Guid? TransitionId { get; init; }
}

public sealed class SaveTransitionCommandValidator : AbstractValidator<SaveTransitionCommand>
{
    public SaveTransitionCommandValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên bước chuyển.").MaximumLength(250);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.BranchName).MaximumLength(250);
        RuleFor(x => x.FromStatusId).NotEmpty();
        RuleFor(x => x.ToStatusId).NotEmpty();
        RuleFor(x => x.SourceAnchor).Must(BeAnchor).WithMessage("Vị trí mũi tên ra không hợp lệ.");
        RuleFor(x => x.TargetAnchor).Must(BeAnchor).WithMessage("Vị trí mũi tên vào không hợp lệ.");
        RuleFor(x => x.Color).MaximumLength(20);
        RuleFor(x => x.TextColor).MaximumLength(20);
        RuleFor(x => x.DropdownValueType).MaximumLength(100)
            .NotEmpty().When(x => x.IsDropdownRequired).WithMessage("Vui lòng nhập DropdownValueType.");

        RuleFor(x => x.SignatureType)
            .Must(v => string.IsNullOrWhiteSpace(v) || ConstWorkflow.Signature.Types.Contains(v.Trim()))
            .WithMessage("Chọn chữ ký không hợp lệ");
        RuleFor(x => x.SignerType)
            .Must(v => v is not null && ConstWorkflow.Signature.Signers.Contains(v.Trim()))
            .When(x => !string.IsNullOrWhiteSpace(x.SignatureType) && x.SignatureType.Trim() != ConstWorkflow.Signature.None)
            .WithMessage("Vui lòng chọn Người ký");

        RuleFor(x => x.AssigneeUpdateMode).MustAsync((mode, ct) => ModeExistsAsync(unitOfWork, mode, ct))
            .WithMessage("Cách cập nhật người được phân công không hợp lệ.");
        RuleFor(x => x.ReporterUpdateMode).MustAsync((mode, ct) => ModeExistsAsync(unitOfWork, mode, ct))
            .WithMessage("Cách cập nhật người theo dõi không hợp lệ.");
        RuleFor(x => x.AssigneeRoleId).NotEmpty().When(x => x.AssigneeUpdateMode == ConstWorkflow.UpdateMode.Roles)
            .WithMessage("Chọn nhóm cho người được phân công.");
        RuleFor(x => x.ReporterRoleId).NotEmpty().When(x => x.ReporterUpdateMode == ConstWorkflow.UpdateMode.Roles)
            .WithMessage("Chọn nhóm cho người theo dõi.");
        RuleFor(x => x.AssigneeValue).MaximumLength(1000);
        RuleFor(x => x.ReporterValue).MaximumLength(1000);
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                var ids = new[] { cmd.PermissionRoleId, cmd.AssigneeRoleId, cmd.ReporterRoleId }
                    .Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
                return ids.Count == 0 || await unitOfWork.Repository<SysRole>().CountAsync(r => ids.Contains(r.Id), ct) == ids.Count;
            })
            .WithName("roles").WithMessage("Nhóm (role) không tồn tại.");

        RuleForEach(x => x.Conditions).ChildRules(c =>
        {
            c.RuleFor(x => x.Connector).Must(v => string.IsNullOrEmpty(v) || ConstWorkflow.Condition.Connectors.Contains(v));
            c.RuleFor(x => x.ConditionType).Must(v => v is not null && ConstWorkflow.Condition.Types.Contains(v))
                .WithMessage("Chọn loại điều kiện (FIELD/TIME).");
            c.RuleFor(x => x.Field).NotEmpty().MaximumLength(250);
            c.RuleFor(x => x.ComparisonType).Must(v => v is not null && ConstWorkflow.Condition.Comparisons.Contains(v))
                .WithMessage("Chọn phép so sánh.");
            c.RuleFor(x => x.ValueType).Must(v => string.IsNullOrEmpty(v) || ConstWorkflow.Condition.ValueTypes.Contains(v));
            c.RuleFor(x => x.Value).MaximumLength(1000);
            c.RuleFor(x => x.SqlText).MaximumLength(2000);
        }).When(x => x.IsAutomatic);

        RuleForEach(x => x.Notifications).ChildRules(n =>
        {
            n.RuleFor(x => x.Type).Must(v => ConstWorkflow.NotificationType.All.Contains(v)).WithMessage("Loại gửi thông báo không hợp lệ.");
            n.RuleFor(x => x.ZnsTemplateId).NotEmpty().MaximumLength(100).When(x => x.Type == ConstWorkflow.NotificationType.Zalo)
                .WithMessage("Nhập ZNS template cho thông báo Zalo.");
            n.RuleFor(x => x.CrmTable).NotEmpty().When(x => x.Type == ConstWorkflow.NotificationType.Zalo && !x.UseDefaultZaloData);
            n.RuleFor(x => x.CrmField).NotEmpty().When(x => x.Type == ConstWorkflow.NotificationType.Zalo && !x.UseDefaultZaloData);
            n.RuleFor(x => x.Title).MaximumLength(250);
            n.RuleFor(x => x.Message).MaximumLength(1000);
            n.RuleFor(x => x)
                .Must(x => x.IsSendCreator || x.IsSendAssignee || x.IsSendMonitor)
                .When(x => x.Type == ConstWorkflow.NotificationType.Push)
                .WithName("recipients").WithMessage("Chọn ít nhất một người nhận push notification.");
            n.RuleForEach(x => x.Attachments).ChildRules(a => a.RuleFor(x => x.Attachment).NotEmpty().MaximumLength(500));
        });
    }

    private static bool BeAnchor(string? anchor) => string.IsNullOrEmpty(anchor) || ConstWorkflow.Anchor.All.Contains(anchor);

    private static async Task<bool> ModeExistsAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, string? mode, CancellationToken ct) =>
        string.IsNullOrEmpty(mode) || await unitOfWork.Repository<TransitionUpdateMode>().AnyAsync(m => m.Code == mode, ct);
}

public sealed class SaveTransitionCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, ICrmMetadataReader crmMetadata)
    : IRequestHandler<SaveTransitionCommand, Guid>
{
    public async Task<Guid> Handle(SaveTransitionCommand request, CancellationToken ct)
    {
        await EnsureStatusesBelongToWorkflowAsync(request, ct);

        StatusTransition transition;
        List<AutoCondition> conditions = [];
        List<TransitionNotification> notifications = [];
        if (request.TransitionId is { } id)
        {
            transition = await unitOfWork.Repository<StatusTransition>()
                             .FirstOrDefaultAsync(t => t.Id == id && t.WorkflowId == request.WorkflowId, ct)
                         ?? throw new NotFoundException("Bước chuyển", id);
            conditions = await unitOfWork.Repository<AutoCondition>().Where(c => c.TransitionId == id).ToListAsync(ct);
            notifications = await unitOfWork.Repository<TransitionNotification>().Where(n => n.TransitionId == id).ToListAsync(ct);
        }
        else
        {
            transition = new StatusTransition { WorkflowId = request.WorkflowId, Name = request.Name };
            unitOfWork.Repository<StatusTransition>().Add(transition);
        }

        Apply(transition, request);
        SyncConditions(transition.Id, conditions, request.IsAutomatic ? request.Conditions ?? [] : []);
        await SyncNotificationsAsync(transition.Id, notifications, request.Notifications ?? [], ct);

        await unitOfWork.SaveChangesAsync(ct);
        return transition.Id;
    }

    private async Task EnsureStatusesBelongToWorkflowAsync(SaveTransitionCommand request, CancellationToken ct)
    {
        if (!await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Id == request.WorkflowId, ct))
            throw new NotFoundException("Workflow", request.WorkflowId);

        var ids = new[] { request.FromStatusId, request.ToStatusId }.Distinct().ToList();
        var found = await unitOfWork.Repository<WorkflowStatus>().CountAsync(s => s.WorkflowId == request.WorkflowId && ids.Contains(s.Id), ct);
        if (found != ids.Count)
            throw new ValidationException(nameof(request.FromStatusId), "Trạng thái nguồn/đích không thuộc workflow này.");
    }

    private static void Apply(StatusTransition t, SaveTransitionCommand r)
    {
        t.Name = r.Name.Trim();
        t.Description = r.Description?.Trim();
        t.OrderIndex = r.OrderIndex;
        t.SetBranch(r.BranchName);
        t.FromStatusId = r.FromStatusId;
        t.ToStatusId = r.ToStatusId;
        t.SourceAnchor = NullIfEmpty(r.SourceAnchor);
        t.TargetAnchor = NullIfEmpty(r.TargetAnchor);
        t.Color = NullIfEmpty(r.Color);
        t.TextColor = NullIfEmpty(r.TextColor);
        t.PermissionRoleId = r.PermissionRoleId;
        t.IsCreatorAllowed = r.IsCreatorAllowed;
        t.IsAssigneeAllowed = r.IsAssigneeAllowed;
        t.IsReporterAllowed = r.IsReporterAllowed;
        t.IsCommentShown = r.IsCommentShown;
        t.IsCommentRequired = r.IsCommentRequired;
        t.IsDropdownShown = r.IsDropdownShown;
        t.IsDropdownRequired = r.IsDropdownRequired;
        t.DropdownValueType = NullIfEmpty(r.DropdownValueType);
        t.IsAutomatic = r.IsAutomatic;

        (t.AssigneeUpdateMode, t.AssigneeRoleId, t.AssigneeValue) = NormalizeUpdateMode(r.AssigneeUpdateMode, r.AssigneeRoleId, r.AssigneeValue);
        (t.ReporterUpdateMode, t.ReporterRoleId, t.ReporterValue) = NormalizeUpdateMode(r.ReporterUpdateMode, r.ReporterRoleId, r.ReporterValue);

        // Không ký (hoặc chưa chọn) → bỏ Người ký; ký nháy / chứng thư số giữ Người ký (validator đã bắt buộc).
        var signature = NullIfEmpty(r.SignatureType);
        t.SignatureType = signature;
        t.SignerType = signature is null || signature == ConstWorkflow.Signature.None ? null : NullIfEmpty(r.SignerType);
    }

    /// <summary>Chỉ giữ giá trị khớp với cách cập nhật: Roles → role; Department/Employee → chuỗi giá trị; còn lại xóa trống.</summary>
    private static (string? Mode, Guid? RoleId, string? Value) NormalizeUpdateMode(string? mode, Guid? roleId, string? value) => mode switch
    {
        ConstWorkflow.UpdateMode.Roles => (mode, roleId, null),
        ConstWorkflow.UpdateMode.Department or ConstWorkflow.UpdateMode.Employee => (mode, null, NullIfEmpty(value)),
        _ => (NullIfEmpty(mode), null, null)
    };

    private void SyncConditions(Guid transitionId, List<AutoCondition> existing, IReadOnlyList<AutoConditionInput> inputs)
    {
        ChildSync.Apply(unitOfWork.Repository<AutoCondition>(), existing, inputs, c => c.Id, i => i.Id,
            (input, index) => Fill(new AutoCondition { TransitionId = transitionId }, input, index),
            (entity, input, index) => Fill(entity, input, index));

        static AutoCondition Fill(AutoCondition c, AutoConditionInput i, int index)
        {
            c.OrderIndex = index;
            c.Connector = NullIfEmpty(i.Connector);
            c.ConditionType = i.ConditionType;
            c.Field = i.Field?.Trim();
            c.ComparisonType = i.ComparisonType;
            c.ValueType = NullIfEmpty(i.ValueType) ?? ConstWorkflow.Condition.ValueInput;
            c.Value = i.Value?.Trim();
            // Như form cũ: SQLText tự ghép từ Connector + Field + Comparison + Value, người dùng có thể sửa tay.
            c.SqlText = NullIfEmpty(i.SqlText) ?? $"{c.Connector}{c.Field}{c.ComparisonType}{c.Value}";
            return c;
        }
    }

    private async Task SyncNotificationsAsync(
        Guid transitionId, List<TransitionNotification> existing, IReadOnlyList<TransitionNotificationInput> inputs, CancellationToken ct)
    {
        var existingIds = existing.Select(n => n.Id).ToList();
        var recipients = await unitOfWork.Repository<NotificationRecipient>().Where(r => existingIds.Contains(r.NotificationId)).ToListAsync(ct);
        var attachments = await unitOfWork.Repository<NotificationAttachment>().Where(a => existingIds.Contains(a.NotificationId)).ToListAsync(ct);

        var schemas = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in inputs.Where(i => i.Type == ConstWorkflow.NotificationType.Zalo).Select(ZaloTable).Distinct())
            schemas[table] = await crmMetadata.GetSchemaAsync(table, ct);

        foreach (var input in inputs.Where(i => i.Type == ConstWorkflow.NotificationType.Zalo && !i.UseDefaultZaloData))
        {
            if (schemas[ZaloTable(input)] is null)
                throw new ValidationException("notifications", $"Bảng \"{input.CrmTable}\" không nằm trong danh sách được phép.");
            if (!(await crmMetadata.GetColumnsAsync(input.CrmTable!, ct)).Contains(input.CrmField!, StringComparer.OrdinalIgnoreCase))
                throw new ValidationException("notifications", $"Cột \"{input.CrmField}\" không thuộc bảng \"{input.CrmTable}\".");
        }

        var keptIds = new HashSet<Guid>();
        var byId = existing.ToDictionary(n => n.Id);
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (input.Id is { } id && byId.TryGetValue(id, out var notification))
            {
                keptIds.Add(id);
            }
            else
            {
                notification = new TransitionNotification { TransitionId = transitionId, Type = input.Type };
                unitOfWork.Repository<TransitionNotification>().Add(notification);
            }

            FillNotification(notification, input, i, schemas);
            var isEmail = input.Type == ConstWorkflow.NotificationType.Email;
            SyncRecipients(notification.Id, recipients.Where(r => r.NotificationId == notification.Id && r.Kind == ConstWorkflow.RecipientKind.Cc).ToList(),
                isEmail ? input.Cc ?? [] : [], ConstWorkflow.RecipientKind.Cc);
            SyncRecipients(notification.Id, recipients.Where(r => r.NotificationId == notification.Id && r.Kind == ConstWorkflow.RecipientKind.Bcc).ToList(),
                isEmail ? input.Bcc ?? [] : [], ConstWorkflow.RecipientKind.Bcc);
            ChildSync.Apply(unitOfWork.Repository<NotificationAttachment>(),
                attachments.Where(a => a.NotificationId == notification.Id).ToList(), isEmail ? input.Attachments ?? [] : [],
                a => a.Id, a => a.Id,
                (a, _) => new NotificationAttachment { NotificationId = notification.Id, Attachment = a.Attachment.Trim() },
                (entity, a, _) => entity.Attachment = a.Attachment.Trim());
        }

        var removed = existing.Where(n => !keptIds.Contains(n.Id)).ToList();
        var removedIds = removed.Select(n => n.Id).ToHashSet();
        unitOfWork.Repository<NotificationRecipient>().RemoveRange(recipients.Where(r => removedIds.Contains(r.NotificationId)));
        unitOfWork.Repository<NotificationAttachment>().RemoveRange(attachments.Where(a => removedIds.Contains(a.NotificationId)));
        unitOfWork.Repository<TransitionNotification>().RemoveRange(removed);
    }

    private static string ZaloTable(TransitionNotificationInput input) =>
        input.UseDefaultZaloData ? ConstWorkflow.ZaloDefault.Table : input.CrmTable!.Trim();

    /// <summary>Chỉ giữ field của đúng loại thông báo — tránh dữ liệu thừa khi người dùng đổi loại trên form.</summary>
    private static void FillNotification(TransitionNotification n, TransitionNotificationInput i, int index, Dictionary<string, string?> schemas)
    {
        n.OrderIndex = index;
        n.Type = i.Type;
        n.Mode = NullIfEmpty(i.Mode);
        n.ConfigValue = NullIfEmpty(i.ConfigValue);
        n.TemplateId = i.TemplateId;

        var isZalo = i.Type == ConstWorkflow.NotificationType.Zalo;
        n.ZnsTemplateId = isZalo ? i.ZnsTemplateId?.Trim() : null;
        n.CrmTable = isZalo ? ZaloTable(i) : null;
        n.CrmField = isZalo ? (i.UseDefaultZaloData ? ConstWorkflow.ZaloDefault.Field : i.CrmField!.Trim()) : null;
        n.CrmSchema = isZalo ? schemas.GetValueOrDefault(n.CrmTable!) : null;

        var isPush = i.Type == ConstWorkflow.NotificationType.Push;
        n.IsSendCreator = isPush && i.IsSendCreator;
        n.IsSendAssignee = isPush && i.IsSendAssignee;
        n.IsSendMonitor = isPush && i.IsSendMonitor;
        n.Title = isPush ? i.Title?.Trim() : null;
        n.Message = isPush ? i.Message?.Trim() : null;
    }

    private void SyncRecipients(Guid notificationId, List<NotificationRecipient> existing, IReadOnlyList<NotificationRecipientInput> inputs, string kind) =>
        ChildSync.Apply(unitOfWork.Repository<NotificationRecipient>(), existing, inputs, r => r.Id, r => r.Id,
            (input, _) => new NotificationRecipient { NotificationId = notificationId, Kind = kind, Mode = NullIfEmpty(input.Mode), ConfigValue = NullIfEmpty(input.ConfigValue) },
            (entity, input, _) =>
            {
                entity.Mode = NullIfEmpty(input.Mode);
                entity.ConfigValue = NullIfEmpty(input.ConfigValue);
            });

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
