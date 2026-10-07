using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WorkflowConfig.Domain.Common;
using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.Persistence;

public class WorkflowConfigDbContext(DbContextOptions<WorkflowConfigDbContext> options) : DbContext(options)
{
    // Phân quyền 6 bảng
    public DbSet<SysAccount> SysAccounts => Set<SysAccount>();
    public DbSet<SysRole> SysRoles => Set<SysRole>();
    public DbSet<SysActivity> SysActivities => Set<SysActivity>();
    public DbSet<SysUserRole> SysUserRoles => Set<SysUserRole>();
    public DbSet<SysRoleActivity> SysRoleActivities => Set<SysRoleActivity>();
    public DbSet<SysUserActivity> SysUserActivities => Set<SysUserActivity>();

    // Hệ thống
    public DbSet<SysRefreshToken> SysRefreshTokens => Set<SysRefreshToken>();
    public DbSet<SysLogApi> SysLogApis => Set<SysLogApi>();

    // Nghiệp vụ: không bắt buộc khai DbSet — handler dùng unitOfWork.Repository<T>().
    // Chỉ cần thêm entity + IEntityTypeConfiguration<T> trong Configurations/<Business>/.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowConfigDbContext).Assembly);
        ApplyBaseEntityConventions(modelBuilder);
    }

    /// <summary>Mọi BaseEntity: global query filter !IsDeleted + kiểu cột audit thống nhất.</summary>
    private static void ApplyBaseEntityConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType) && t.BaseType is null))
        {
            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property(nameof(BaseEntity.CreatedName)).HasMaxLength(100);
            builder.Property(nameof(BaseEntity.Updater)).HasMaxLength(100);
            // INCLUDE IsDeleted để sort/phân trang theo CreatedDate không phải key lookup (RULES 4.10).
            builder.HasIndex(nameof(BaseEntity.CreatedDate)).IncludeProperties(nameof(BaseEntity.IsDeleted));

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var notDeleted = Expression.Lambda(
                Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted))), parameter);
            builder.HasQueryFilter(notDeleted);
        }
    }
}
