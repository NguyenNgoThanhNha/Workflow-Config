using System.Text.Json.Serialization;
using FluentValidation;
using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Commands.CopyWorkflow;

/// <summary>
/// Nhân bản workflow cùng toàn bộ cấu hình: trạng thái, bước chuyển, điều kiện tự động, thông báo (+Cc/Bcc/đính kèm),
/// cấu hình field và quy tắc field theo trạng thái.
/// </summary>
public sealed record CopyWorkflowCommand(string Code, string Name, int? OrderIndex) : IRequest<WorkflowCopiedDto>
{
    [JsonIgnore]
    public Guid SourceId { get; init; }
}

public sealed class CopyWorkflowCommandValidator : AbstractValidator<CopyWorkflowCommand>
{
    public CopyWorkflowCommandValidator(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100)
            .MustAsync(async (code, ct) => !await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Code == code.Trim(), ct))
            .WithMessage("Mã workflow đã tồn tại. Vui lòng nhập mã khác.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250)
            .MustAsync(async (name, ct) => !await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Name == name.Trim(), ct))
            .WithMessage("Tên workflow đã tồn tại. Vui lòng nhập tên khác.");
        RuleFor(x => x.OrderIndex).NotNull().WithMessage("Vui lòng nhập thứ tự hiển thị.")
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải là số nguyên không âm.");
    }
}

public sealed class CopyWorkflowCommandHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<CopyWorkflowCommand, WorkflowCopiedDto>
{
    public async Task<WorkflowCopiedDto> Handle(CopyWorkflowCommand request, CancellationToken ct)
    {
        var source = await unitOfWork.Repository<Workflow>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.SourceId, ct)
                     ?? throw new NotFoundException("Workflow", request.SourceId);

        var code = request.Code.Trim();
        var name = request.Name.Trim();
        if (string.Equals(code, source.Code, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, source.Name, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(nameof(request.Code), "Mã và tên workflow mới phải khác workflow gốc.");

        var sourceId = source.Id;
        var statuses = await unitOfWork.Repository<WorkflowStatus>().AsNoTracking().Where(s => s.WorkflowId == sourceId).ToListAsync(ct);
        var transitions = await unitOfWork.Repository<StatusTransition>().AsNoTracking().Where(t => t.WorkflowId == sourceId).ToListAsync(ct);
        var transitionIds = transitions.Select(t => t.Id).ToList();
        var conditions = await unitOfWork.Repository<AutoCondition>().AsNoTracking()
            .Where(c => transitionIds.Contains(c.TransitionId)).ToListAsync(ct);
        var notifications = await unitOfWork.Repository<TransitionNotification>().AsNoTracking()
            .Where(n => transitionIds.Contains(n.TransitionId)).ToListAsync(ct);
        var notificationIds = notifications.Select(n => n.Id).ToList();
        var recipients = await unitOfWork.Repository<NotificationRecipient>().AsNoTracking()
            .Where(r => notificationIds.Contains(r.NotificationId)).ToListAsync(ct);
        var attachments = await unitOfWork.Repository<NotificationAttachment>().AsNoTracking()
            .Where(a => notificationIds.Contains(a.NotificationId)).ToListAsync(ct);
        var fieldConfigs = await unitOfWork.Repository<WorkflowFieldConfig>().AsNoTracking()
            .Where(f => f.WorkflowId == sourceId).ToListAsync(ct);
        var fieldRules = await unitOfWork.Repository<WorkflowStatusFieldRule>().AsNoTracking()
            .Where(r => r.WorkflowId == sourceId).ToListAsync(ct);

        var copy = new Workflow
        {
            Code = code, Name = name, OrderIndex = request.OrderIndex!.Value,
            CategoryCode = source.CategoryCode, CompanyCode = source.CompanyCode, ImagePath = source.ImagePath,
            IsActive = source.IsActive, IsSummaryDisabled = source.IsSummaryDisabled
        };
        copy.RefreshSearchText();
        unitOfWork.Repository<Workflow>().Add(copy);

        var statusMap = new Dictionary<Guid, Guid>();
        foreach (var s in statuses)
        {
            var clone = new WorkflowStatus
            {
                WorkflowId = copy.Id, Code = s.Code, Name = s.Name, OrderIndex = s.OrderIndex, Category = s.Category,
                ProcessCode = s.ProcessCode, PositionX = s.PositionX, PositionY = s.PositionY, AutoUpdateEndDate = s.AutoUpdateEndDate,
                IsPushNotification = s.IsPushNotification, IsSendCreator = s.IsSendCreator, IsSendAssignee = s.IsSendAssignee,
                IsSendMonitor = s.IsSendMonitor, NotificationTitle = s.NotificationTitle, NotificationMessage = s.NotificationMessage,
                TextColor = s.TextColor, BackgroundColor = s.BackgroundColor, CustomColor = s.CustomColor, IsActive = s.IsActive
            };
            statusMap[s.Id] = clone.Id;
            unitOfWork.Repository<WorkflowStatus>().Add(clone);
        }

        var notificationsByTransition = notifications.ToLookup(n => n.TransitionId);
        var conditionsByTransition = conditions.ToLookup(c => c.TransitionId);
        var recipientsByNotification = recipients.ToLookup(r => r.NotificationId);
        var attachmentsByNotification = attachments.ToLookup(a => a.NotificationId);

        // Bước chuyển trỏ tới trạng thái không còn (dữ liệu hỏng) thì bỏ qua.
        foreach (var t in transitions.Where(t => statusMap.ContainsKey(t.FromStatusId) && statusMap.ContainsKey(t.ToStatusId)))
        {
            var clone = new StatusTransition
            {
                WorkflowId = copy.Id, FromStatusId = statusMap[t.FromStatusId], ToStatusId = statusMap[t.ToStatusId], Name = t.Name,
                Description = t.Description, OrderIndex = t.OrderIndex, BranchName = t.BranchName, BranchKey = t.BranchKey,
                BranchPositionX = t.BranchPositionX, BranchPositionY = t.BranchPositionY, SourceAnchor = t.SourceAnchor,
                TargetAnchor = t.TargetAnchor, Color = t.Color, TextColor = t.TextColor, PermissionRoleId = t.PermissionRoleId,
                IsCreatorAllowed = t.IsCreatorAllowed, IsAssigneeAllowed = t.IsAssigneeAllowed, IsReporterAllowed = t.IsReporterAllowed,
                IsCommentShown = t.IsCommentShown, IsCommentRequired = t.IsCommentRequired, IsDropdownShown = t.IsDropdownShown,
                IsDropdownRequired = t.IsDropdownRequired, DropdownValueType = t.DropdownValueType, IsAutomatic = t.IsAutomatic,
                AssigneeUpdateMode = t.AssigneeUpdateMode, AssigneeRoleId = t.AssigneeRoleId, AssigneeValue = t.AssigneeValue,
                ReporterUpdateMode = t.ReporterUpdateMode, ReporterRoleId = t.ReporterRoleId, ReporterValue = t.ReporterValue,
                SignatureType = t.SignatureType, SignerType = t.SignerType
            };

            foreach (var c in conditionsByTransition[t.Id])
            {
                clone.Conditions.Add(new AutoCondition
                {
                    TransitionId = clone.Id, OrderIndex = c.OrderIndex, Connector = c.Connector, ConditionType = c.ConditionType,
                    Field = c.Field, ComparisonType = c.ComparisonType, ValueType = c.ValueType, Value = c.Value, SqlText = c.SqlText
                });
            }

            foreach (var n in notificationsByTransition[t.Id])
            {
                var notification = new TransitionNotification
                {
                    TransitionId = clone.Id, OrderIndex = n.OrderIndex, Type = n.Type, Mode = n.Mode, ConfigValue = n.ConfigValue,
                    TemplateId = n.TemplateId, ZnsTemplateId = n.ZnsTemplateId, CrmSchema = n.CrmSchema, CrmTable = n.CrmTable,
                    CrmField = n.CrmField, IsSendCreator = n.IsSendCreator, IsSendAssignee = n.IsSendAssignee,
                    IsSendMonitor = n.IsSendMonitor, Title = n.Title, Message = n.Message
                };
                foreach (var r in recipientsByNotification[n.Id])
                    notification.Recipients.Add(new NotificationRecipient
                    {
                        NotificationId = notification.Id, Kind = r.Kind, Mode = r.Mode, ConfigValue = r.ConfigValue
                    });
                foreach (var a in attachmentsByNotification[n.Id])
                    notification.Attachments.Add(new NotificationAttachment { NotificationId = notification.Id, Attachment = a.Attachment });
                clone.Notifications.Add(notification);
            }

            unitOfWork.Repository<StatusTransition>().Add(clone);
        }

        foreach (var f in fieldConfigs)
        {
            unitOfWork.Repository<WorkflowFieldConfig>().Add(new WorkflowFieldConfig
            {
                WorkflowId = copy.Id, FieldCode = f.FieldCode, IsRequired = f.IsRequired, OrderIndex = f.OrderIndex,
                Parameters = f.Parameters, Note = f.Note, NoteEn = f.NoteEn, HideWhenAdd = f.HideWhenAdd,
                AddDefaultValue = f.AddDefaultValue, HideWhenEdit = f.HideWhenEdit, EditDefaultValue = f.EditDefaultValue
            });
        }

        foreach (var r in fieldRules.Where(r => statusMap.ContainsKey(r.StatusId)))
        {
            unitOfWork.Repository<WorkflowStatusFieldRule>().Add(new WorkflowStatusFieldRule
            {
                WorkflowId = copy.Id, StatusId = statusMap[r.StatusId], FieldCode = r.FieldCode,
                DisableForCreator = r.DisableForCreator, RequiredForCreator = r.RequiredForCreator,
                DisableForAssignee = r.DisableForAssignee, RequiredForAssignee = r.RequiredForAssignee,
                DisableForReporter = r.DisableForReporter, RequiredForReporter = r.RequiredForReporter
            });
        }

        // Một lần SaveChanges: FK đã khai trong model nên EF tự sắp thứ tự INSERT (cha trước con).
        await unitOfWork.SaveChangesAsync(ct);
        return new WorkflowCopiedDto(copy.Id, copy.Code, copy.Name);
    }
}
