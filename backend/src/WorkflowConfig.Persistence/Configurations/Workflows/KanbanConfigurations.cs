using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Workflows;

namespace WorkflowConfig.Persistence.Configurations.Workflows;

public class KanbanConfiguration : IEntityTypeConfiguration<Kanban>
{
    public void Configure(EntityTypeBuilder<Kanban> builder)
    {
        builder.ToTable(ConstTable.Kanban);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(WorkflowColumn.Short).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Name).IsRequired();
        builder.Property(x => x.SearchText).HasMaxLength(400).UseCollation(WorkflowColumn.BinaryCollation).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.OrderIndex).IncludeProperties(x => new { x.IsActive, x.IsDeleted });
        builder.HasMany(x => x.Columns).WithOne().HasForeignKey(x => x.KanbanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class KanbanColumnConfiguration : IEntityTypeConfiguration<KanbanColumn>
{
    public void Configure(EntityTypeBuilder<KanbanColumn> builder)
    {
        builder.ToTable(ConstTable.KanbanColumn);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(WorkflowColumn.Name).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(WorkflowColumn.Text);
        builder.Property(x => x.Color).HasMaxLength(WorkflowColumn.Color);
        builder.HasIndex(x => new { x.KanbanId, x.OrderIndex }).IncludeProperties(x => x.IsDeleted);
    }
}

public class KanbanStatusMappingConfiguration : IEntityTypeConfiguration<KanbanStatusMapping>
{
    public void Configure(EntityTypeBuilder<KanbanStatusMapping> builder)
    {
        builder.ToTable(ConstTable.KanbanStatusMapping);
        builder.HasKey(x => x.Id);
        builder.HasOne<Kanban>().WithMany().HasForeignKey(x => x.KanbanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KanbanColumn>().WithMany().HasForeignKey(x => x.ColumnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowStatus>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);

        // Một trạng thái chỉ nằm ở một cột trong mỗi bảng.
        builder.HasIndex(x => new { x.KanbanId, x.StatusId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.ColumnId).IncludeProperties(x => x.IsDeleted);
        builder.HasIndex(x => x.StatusId).IncludeProperties(x => x.IsDeleted);
    }
}
