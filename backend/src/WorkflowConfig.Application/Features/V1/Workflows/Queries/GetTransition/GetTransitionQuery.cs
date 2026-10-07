using WorkflowConfig.Application.Features.V1.Workflows.DTOs;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Application.Features.V1.Workflows.Queries.GetTransition;

/// <summary>Chi tiết bước chuyển cho form: thông tin, điều kiện tự động, cấu hình thông báo.</summary>
public sealed record GetTransitionQuery(Guid WorkflowId, Guid TransitionId) : IRequest<TransitionDetailDto>;

public sealed class GetTransitionQueryHandler(IUnitOfWork<WorkflowConfigDbContext> unitOfWork)
    : IRequestHandler<GetTransitionQuery, TransitionDetailDto>
{
    public async Task<TransitionDetailDto> Handle(GetTransitionQuery request, CancellationToken ct)
    {
        var t = await unitOfWork.Repository<StatusTransition>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TransitionId && x.WorkflowId == request.WorkflowId, ct)
            ?? throw new NotFoundException("Bước chuyển", request.TransitionId);

        var conditions = await unitOfWork.Repository<AutoCondition>().AsNoTracking()
            .Where(c => c.TransitionId == t.Id)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new AutoConditionDto(c.Id, c.Connector, c.ConditionType, c.Field, c.ComparisonType, c.ValueType, c.Value, c.SqlText))
            .ToListAsync(ct);

        var notifications = await unitOfWork.Repository<TransitionNotification>().AsNoTracking()
            .Where(n => n.TransitionId == t.Id)
            .OrderBy(n => n.OrderIndex)
            .ToListAsync(ct);
        var notificationIds = notifications.Select(n => n.Id).ToList();
        var recipients = (await unitOfWork.Repository<NotificationRecipient>().AsNoTracking()
                .Where(r => notificationIds.Contains(r.NotificationId))
                .OrderBy(r => r.CreatedDate)
                .ToListAsync(ct))
            .ToLookup(r => r.NotificationId);
        var attachments = (await unitOfWork.Repository<NotificationAttachment>().AsNoTracking()
                .Where(a => notificationIds.Contains(a.NotificationId))
                .OrderBy(a => a.CreatedDate)
                .ToListAsync(ct))
            .ToLookup(a => a.NotificationId);

        var notificationDtos = notifications.Select(n => new TransitionNotificationDto(
            n.Id, n.Type, n.Mode, n.ConfigValue, n.TemplateId, n.ZnsTemplateId, n.CrmSchema, n.CrmTable, n.CrmField,
            n.IsSendCreator, n.IsSendAssignee, n.IsSendMonitor, n.Title, n.Message,
            Recipients(recipients[n.Id], ConstWorkflow.RecipientKind.Cc),
            Recipients(recipients[n.Id], ConstWorkflow.RecipientKind.Bcc),
            attachments[n.Id].Select(a => new NotificationAttachmentDto(a.Id, a.Attachment)).ToList())).ToList();

        return new TransitionDetailDto(
            t.Id, t.WorkflowId, t.FromStatusId, t.ToStatusId, t.Name, t.Description, t.OrderIndex, t.BranchName, t.SourceAnchor,
            t.TargetAnchor, t.Color, t.TextColor, t.PermissionRoleId, t.IsCreatorAllowed, t.IsAssigneeAllowed, t.IsReporterAllowed,
            t.IsCommentShown, t.IsCommentRequired, t.IsDropdownShown, t.IsDropdownRequired, t.DropdownValueType, t.IsAutomatic,
            t.AssigneeUpdateMode, t.AssigneeRoleId, t.AssigneeValue, t.ReporterUpdateMode, t.ReporterRoleId, t.ReporterValue,
            t.SignatureType, t.SignerType, conditions, notificationDtos);
    }

    private static List<NotificationRecipientDto> Recipients(IEnumerable<NotificationRecipient> source, string kind) =>
        source.Where(r => r.Kind == kind).Select(r => new NotificationRecipientDto(r.Id, r.Mode, r.ConfigValue)).ToList();
}
