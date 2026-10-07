using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Sys;

namespace WorkflowConfig.Persistence.Configurations.Sys;

public class SysRefreshTokenConfiguration : IEntityTypeConfiguration<SysRefreshToken>
{
    public void Configure(EntityTypeBuilder<SysRefreshToken> builder)
    {
        builder.ToTable(ConstTable.RefreshToken);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasColumnType("varchar(64)").IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasColumnType("varchar(64)");
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.ExpiresAt); // job dọn token hết hạn (DataRetention)
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
