namespace WorkflowConfig.Application.Features.V1.Roles.DTOs;

/// <summary>Actions = quyền áp dụng ("CRUD", "R"...); IsSystem = chức năng khai trong code (không sửa/xóa trên giao diện).</summary>
public sealed record ActivityDto(Guid Id, string Code, string Name, string? Description, string Actions, bool IsSystem);

public sealed record ActivityPermissionInput(Guid ActivityId, bool C, bool R, bool U, bool D)
{
    public bool Any => C || R || U || D;
}

public sealed record ActivityPermissionDto(Guid ActivityId, string Code, string Name, bool C, bool R, bool U, bool D);

public sealed record RoleRefDto(Guid Id, string Name);

public sealed record RoleDto(Guid Id, string Name, string? Description, bool IsAdmin, int UserCount);

public sealed record RoleDetailDto(
    Guid Id, string Name, string? Description, bool IsAdmin, int UserCount, IReadOnlyList<ActivityPermissionDto> Activities);
