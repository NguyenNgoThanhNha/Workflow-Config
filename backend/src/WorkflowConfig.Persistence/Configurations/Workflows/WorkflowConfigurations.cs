using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Persistence.Configurations.Workflows;

internal static class WorkflowColumn
{
    public const int Code = 50;
    public const int Name = 250;
    public const int Short = 100;
    public const int Color = 20;
    public const int Text = 1000;
    public const int Sql = 2000;
    public const string BinaryCollation = "Latin1_General_100_BIN2";
}

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable(ConstTable.Workflow);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(WorkflowColumn.Short).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Name).IsRequired();
        builder.Property(x => x.CategoryCode).HasMaxLength(WorkflowColumn.Short);
        builder.Property(x => x.CompanyCode).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.ImagePath).HasMaxLength(500);
        builder.Property(x => x.SearchText).HasMaxLength(400).UseCollation(WorkflowColumn.BinaryCollation).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.OrderIndex).IncludeProperties(x => new { x.IsActive, x.IsDeleted });

        builder.HasMany(x => x.Statuses).WithOne(x => x.Workflow).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Transitions).WithOne(x => x.Workflow).HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.FieldConfigs).WithOne().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowStatusConfiguration : IEntityTypeConfiguration<WorkflowStatus>
{
    public void Configure(EntityTypeBuilder<WorkflowStatus> builder)
    {
        builder.ToTable(ConstTable.WorkflowStatus);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(WorkflowColumn.Short).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Name).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(WorkflowColumn.Short);
        builder.Property(x => x.ProcessCode).HasMaxLength(WorkflowColumn.Code).IsRequired();
        builder.Property(x => x.NotificationTitle).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.NotificationMessage).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.TextColor).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.BackgroundColor).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.CustomColor).HasMaxLength(WorkflowColumn.Color);

        builder.HasIndex(x => new { x.WorkflowId, x.OrderIndex }).IncludeProperties(x => x.IsDeleted);
        builder.HasMany(x => x.FieldRules).WithOne().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StatusTransitionConfiguration : IEntityTypeConfiguration<StatusTransition>
{
    public void Configure(EntityTypeBuilder<StatusTransition> builder)
    {
        builder.ToTable(ConstTable.StatusTransition);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.BranchName).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.BranchKey).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.SourceAnchor).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.TargetAnchor).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.Color).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.TextColor).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.DropdownValueType).HasMaxLength(WorkflowColumn.Short);
        builder.Property(x => x.AssigneeUpdateMode).HasMaxLength(WorkflowColumn.Code);
        builder.Property(x => x.AssigneeValue).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.ReporterUpdateMode).HasMaxLength(WorkflowColumn.Code);
        builder.Property(x => x.ReporterValue).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.SignatureType).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.SignerType).HasMaxLength(WorkflowColumn.Color);

        // Hai FK cùng trỏ Wf_Status → không cascade (SQL Server cấm nhiều đường cascade); xóa trạng thái bị chặn ở handler.
        builder.HasOne(x => x.FromStatus).WithMany().HasForeignKey(x => x.FromStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToStatus).WithMany().HasForeignKey(x => x.ToStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Conditions).WithOne().HasForeignKey(x => x.TransitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Notifications).WithOne().HasForeignKey(x => x.TransitionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.WorkflowId).IncludeProperties(x => x.IsDeleted);
        builder.HasIndex(x => new { x.FromStatusId, x.BranchKey }).IncludeProperties(x => x.IsDeleted);
        builder.HasIndex(x => x.ToStatusId).IncludeProperties(x => x.IsDeleted);
    }
}

public class AutoConditionConfiguration : IEntityTypeConfiguration<AutoCondition>
{
    public void Configure(EntityTypeBuilder<AutoCondition> builder)
    {
        builder.ToTable(ConstTable.AutoCondition);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Connector).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.ConditionType).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.Field).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.ComparisonType).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.ValueType).HasMaxLength(WorkflowColumn.Color);
        builder.Property(x => x.Value).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.SqlText).HasMaxLength(WorkflowColumn.Sql);
        builder.HasIndex(x => x.TransitionId).IncludeProperties(x => x.IsDeleted);
    }
}

public class TransitionNotificationConfiguration : IEntityTypeConfiguration<TransitionNotification>
{
    public void Configure(EntityTypeBuilder<TransitionNotification> builder)
    {
        builder.ToTable(ConstTable.TransitionNotification);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(WorkflowColumn.Code).IsRequired();
        builder.Property(x => x.Mode).HasMaxLength(WorkflowColumn.Code);
        builder.Property(x => x.ConfigValue).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.ZnsTemplateId).HasMaxLength(WorkflowColumn.Short);
        builder.Property(x => x.CrmSchema).HasMaxLength(128);
        builder.Property(x => x.CrmTable).HasMaxLength(128);
        builder.Property(x => x.CrmField).HasMaxLength(128);
        builder.Property(x => x.Title).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.Message).HasMaxLength(WorkflowColumn.Text);

        builder.HasMany(x => x.Recipients).WithOne().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TransitionId).IncludeProperties(x => x.IsDeleted);
    }
}

public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable(ConstTable.NotificationRecipient);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Mode).HasMaxLength(WorkflowColumn.Code);
        builder.Property(x => x.ConfigValue).HasMaxLength(WorkflowColumn.Name);
        builder.HasIndex(x => x.NotificationId).IncludeProperties(x => x.IsDeleted);
    }
}

public class NotificationAttachmentConfiguration : IEntityTypeConfiguration<NotificationAttachment>
{
    public void Configure(EntityTypeBuilder<NotificationAttachment> builder)
    {
        builder.ToTable(ConstTable.NotificationAttachment);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Attachment).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.NotificationId).IncludeProperties(x => x.IsDeleted);
    }
}

public class WorkflowFieldConfiguration : IEntityTypeConfiguration<WorkflowField>
{
    public void Configure(EntityTypeBuilder<WorkflowField> builder)
    {
        builder.ToTable(ConstTable.WorkflowField);
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(WorkflowColumn.Short);
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Name).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(WorkflowColumn.Text);
    }
}

public class WorkflowFieldConfigConfiguration : IEntityTypeConfiguration<WorkflowFieldConfig>
{
    public void Configure(EntityTypeBuilder<WorkflowFieldConfig> builder)
    {
        builder.ToTable(ConstTable.WorkflowFieldConfig);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FieldCode).HasMaxLength(WorkflowColumn.Short).IsRequired();
        builder.Property(x => x.Parameters).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.Note).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.NoteEn).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.AddDefaultValue).HasMaxLength(WorkflowColumn.Name);
        builder.Property(x => x.EditDefaultValue).HasMaxLength(WorkflowColumn.Name);

        builder.HasOne<WorkflowField>().WithMany().HasForeignKey(x => x.FieldCode).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkflowId, x.FieldCode }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class WorkflowStatusFieldRuleConfiguration : IEntityTypeConfiguration<WorkflowStatusFieldRule>
{
    public void Configure(EntityTypeBuilder<WorkflowStatusFieldRule> builder)
    {
        builder.ToTable(ConstTable.StatusFieldRule);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FieldCode).HasMaxLength(WorkflowColumn.Short).IsRequired();
        builder.Ignore(x => x.HasAnyFlag);
        builder.HasOne<Workflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.StatusId, x.FieldCode }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class WorkflowProcessConfiguration : IEntityTypeConfiguration<WorkflowProcess>
{
    public void Configure(EntityTypeBuilder<WorkflowProcess> builder)
    {
        builder.ToTable(ConstTable.WorkflowProcess);
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(WorkflowColumn.Code);
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Short).IsRequired();
        builder.Property(x => x.BackgroundColor).HasMaxLength(WorkflowColumn.Color).IsRequired();
        builder.Property(x => x.TextColor).HasMaxLength(WorkflowColumn.Color).IsRequired();
    }
}

public class TransitionUpdateModeConfiguration : IEntityTypeConfiguration<TransitionUpdateMode>
{
    public void Configure(EntityTypeBuilder<TransitionUpdateMode> builder)
    {
        builder.ToTable(ConstTable.TransitionUpdateMode);
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(WorkflowColumn.Code);
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Short).IsRequired();
    }
}
