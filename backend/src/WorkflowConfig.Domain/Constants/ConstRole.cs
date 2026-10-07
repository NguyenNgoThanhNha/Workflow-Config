namespace WorkflowConfig.Domain.Constants;

public static class ConstRole
{
    /// <summary>Giá trị Sys_Role.RoleType của role Admin (toàn quyền, không sửa/xóa được).</summary>
    public const int AdminRoleType = 1;

    public const string Admin = "Admin";
    public const string User = "User";

    /// <summary>Role mặc định cho tài khoản tự đăng ký.</summary>
    public const string DefaultForRegistration = User;

    /// <summary>
    /// Quyền seed mặc định cho role hệ thống — chỉ áp dụng khi role được tạo lần đầu
    /// (sau đó Admin tự chỉnh trên màn quản trị). Admin không cần khai — ngầm định toàn quyền.
    /// Flags: chuỗi gồm các ký tự C/R/U/D.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Flags)>> DefaultPermissions =
        new Dictionary<string, IReadOnlyList<(string, string)>>
        {
            [User] = [(ConstActivity.Workflow, "R")]
        };
}
