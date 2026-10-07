using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowConfig.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sys_Account",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PasswordResetTokenHash = table.Column<string>(type: "varchar(64)", nullable: true),
                    PasswordResetTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Account", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Activity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicationName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Activity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_LogApi",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TraceId = table.Column<string>(type: "varchar(100)", nullable: false),
                    Ip = table.Column<string>(type: "varchar(50)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Method = table.Column<string>(type: "varchar(10)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_LogApi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RoleType = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wf_Field",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_Field", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Wf_Process",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_Process", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Wf_UpdateMode",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_UpdateMode", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Wf_Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSummaryDisabled = table.Column<bool>(type: "bit", nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false, collation: "Latin1_General_100_BIN2"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_Workflow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_RefreshToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "varchar(64)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_RefreshToken_Sys_Account_UserId",
                        column: x => x.UserId,
                        principalTable: "Sys_Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sys_UserActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    C = table.Column<bool>(type: "bit", nullable: false),
                    R = table.Column<bool>(type: "bit", nullable: false),
                    U = table.Column<bool>(type: "bit", nullable: false),
                    D = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_UserActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_UserActivity_Sys_Account_UserId",
                        column: x => x.UserId,
                        principalTable: "Sys_Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sys_UserActivity_Sys_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Sys_Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sys_RoleActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    C = table.Column<bool>(type: "bit", nullable: false),
                    R = table.Column<bool>(type: "bit", nullable: false),
                    U = table.Column<bool>(type: "bit", nullable: false),
                    D = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_RoleActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_RoleActivity_Sys_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Sys_Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sys_RoleActivity_Sys_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Sys_Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sys_UserRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sys_UserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sys_UserRole_Sys_Account_UserId",
                        column: x => x.UserId,
                        principalTable: "Sys_Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sys_UserRole_Sys_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Sys_Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_FieldConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NoteEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HideWhenAdd = table.Column<bool>(type: "bit", nullable: false),
                    AddDefaultValue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HideWhenEdit = table.Column<bool>(type: "bit", nullable: false),
                    EditDefaultValue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_FieldConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_FieldConfig_Wf_Field_FieldCode",
                        column: x => x.FieldCode,
                        principalTable: "Wf_Field",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wf_FieldConfig_Wf_Workflow_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Wf_Workflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_Status",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PositionX = table.Column<int>(type: "int", nullable: true),
                    PositionY = table.Column<int>(type: "int", nullable: true),
                    AutoUpdateEndDate = table.Column<bool>(type: "bit", nullable: false),
                    IsPushNotification = table.Column<bool>(type: "bit", nullable: false),
                    IsSendCreator = table.Column<bool>(type: "bit", nullable: false),
                    IsSendAssignee = table.Column<bool>(type: "bit", nullable: false),
                    IsSendMonitor = table.Column<bool>(type: "bit", nullable: false),
                    NotificationTitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NotificationMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CustomColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_Status", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_Status_Wf_Workflow_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Wf_Workflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_StatusFieldRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisableForCreator = table.Column<bool>(type: "bit", nullable: false),
                    RequiredForCreator = table.Column<bool>(type: "bit", nullable: false),
                    DisableForAssignee = table.Column<bool>(type: "bit", nullable: false),
                    RequiredForAssignee = table.Column<bool>(type: "bit", nullable: false),
                    DisableForReporter = table.Column<bool>(type: "bit", nullable: false),
                    RequiredForReporter = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_StatusFieldRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_StatusFieldRule_Wf_Status_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Wf_Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wf_StatusFieldRule_Wf_Workflow_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Wf_Workflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_StatusTransition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BranchKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BranchPositionX = table.Column<int>(type: "int", nullable: true),
                    BranchPositionY = table.Column<int>(type: "int", nullable: true),
                    SourceAnchor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TargetAnchor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PermissionRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCreatorAllowed = table.Column<bool>(type: "bit", nullable: false),
                    IsAssigneeAllowed = table.Column<bool>(type: "bit", nullable: false),
                    IsReporterAllowed = table.Column<bool>(type: "bit", nullable: false),
                    IsCommentShown = table.Column<bool>(type: "bit", nullable: false),
                    IsCommentRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsDropdownShown = table.Column<bool>(type: "bit", nullable: false),
                    IsDropdownRequired = table.Column<bool>(type: "bit", nullable: false),
                    DropdownValueType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    AssigneeUpdateMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssigneeRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssigneeValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReporterUpdateMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReporterRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReporterValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SignatureType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SignerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_StatusTransition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_StatusTransition_Wf_Status_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "Wf_Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wf_StatusTransition_Wf_Status_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "Wf_Status",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wf_StatusTransition_Wf_Workflow_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Wf_Workflow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_AutoCondition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Connector = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConditionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Field = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ComparisonType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SqlText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_AutoCondition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_AutoCondition_Wf_StatusTransition_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "Wf_StatusTransition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_TransitionNotification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfigValue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ZnsTemplateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CrmSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CrmTable = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CrmField = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsSendCreator = table.Column<bool>(type: "bit", nullable: false),
                    IsSendAssignee = table.Column<bool>(type: "bit", nullable: false),
                    IsSendMonitor = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_TransitionNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_TransitionNotification_Wf_StatusTransition_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "Wf_StatusTransition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_NotificationAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attachment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_NotificationAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_NotificationAttachment_Wf_TransitionNotification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Wf_TransitionNotification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wf_NotificationRecipient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfigValue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Updater = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wf_NotificationRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wf_NotificationRecipient_Wf_TransitionNotification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Wf_TransitionNotification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Account_CreatedDate",
                table: "Sys_Account",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Account_Email",
                table: "Sys_Account",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Activity_Code",
                table: "Sys_Activity",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Activity_CreatedDate",
                table: "Sys_Activity",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_LogApi_CreatedDate",
                table: "Sys_LogApi",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_LogApi_StatusCode_CreatedDate",
                table: "Sys_LogApi",
                columns: new[] { "StatusCode", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_LogApi_TraceId",
                table: "Sys_LogApi",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_LogApi_UserId_CreatedDate",
                table: "Sys_LogApi",
                columns: new[] { "UserId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RefreshToken_CreatedDate",
                table: "Sys_RefreshToken",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RefreshToken_ExpiresAt",
                table: "Sys_RefreshToken",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RefreshToken_TokenHash",
                table: "Sys_RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RefreshToken_UserId",
                table: "Sys_RefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Role_CreatedDate",
                table: "Sys_Role",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_Role_Name",
                table: "Sys_Role",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RoleActivity_ActivityId",
                table: "Sys_RoleActivity",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RoleActivity_CreatedDate",
                table: "Sys_RoleActivity",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_RoleActivity_RoleId_ActivityId",
                table: "Sys_RoleActivity",
                columns: new[] { "RoleId", "ActivityId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_UserActivity_ActivityId",
                table: "Sys_UserActivity",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_UserActivity_CreatedDate",
                table: "Sys_UserActivity",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_UserActivity_UserId_ActivityId",
                table: "Sys_UserActivity",
                columns: new[] { "UserId", "ActivityId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_UserRole_CreatedDate",
                table: "Sys_UserRole",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Sys_UserRole_RoleId",
                table: "Sys_UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sys_UserRole_UserId_RoleId",
                table: "Sys_UserRole",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Wf_AutoCondition_CreatedDate",
                table: "Wf_AutoCondition",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_AutoCondition_TransitionId",
                table: "Wf_AutoCondition",
                column: "TransitionId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Field_CreatedDate",
                table: "Wf_Field",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_FieldConfig_CreatedDate",
                table: "Wf_FieldConfig",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_FieldConfig_FieldCode",
                table: "Wf_FieldConfig",
                column: "FieldCode");

            migrationBuilder.CreateIndex(
                name: "IX_Wf_FieldConfig_WorkflowId_FieldCode",
                table: "Wf_FieldConfig",
                columns: new[] { "WorkflowId", "FieldCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Wf_NotificationAttachment_CreatedDate",
                table: "Wf_NotificationAttachment",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_NotificationAttachment_NotificationId",
                table: "Wf_NotificationAttachment",
                column: "NotificationId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_NotificationRecipient_CreatedDate",
                table: "Wf_NotificationRecipient",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_NotificationRecipient_NotificationId",
                table: "Wf_NotificationRecipient",
                column: "NotificationId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Process_CreatedDate",
                table: "Wf_Process",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Status_CreatedDate",
                table: "Wf_Status",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Status_WorkflowId_OrderIndex",
                table: "Wf_Status",
                columns: new[] { "WorkflowId", "OrderIndex" })
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusFieldRule_CreatedDate",
                table: "Wf_StatusFieldRule",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusFieldRule_StatusId_FieldCode",
                table: "Wf_StatusFieldRule",
                columns: new[] { "StatusId", "FieldCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusFieldRule_WorkflowId",
                table: "Wf_StatusFieldRule",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusTransition_CreatedDate",
                table: "Wf_StatusTransition",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusTransition_FromStatusId_BranchKey",
                table: "Wf_StatusTransition",
                columns: new[] { "FromStatusId", "BranchKey" })
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusTransition_ToStatusId",
                table: "Wf_StatusTransition",
                column: "ToStatusId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_StatusTransition_WorkflowId",
                table: "Wf_StatusTransition",
                column: "WorkflowId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_TransitionNotification_CreatedDate",
                table: "Wf_TransitionNotification",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_TransitionNotification_TransitionId",
                table: "Wf_TransitionNotification",
                column: "TransitionId")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_UpdateMode_CreatedDate",
                table: "Wf_UpdateMode",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Workflow_Code",
                table: "Wf_Workflow",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Workflow_CreatedDate",
                table: "Wf_Workflow",
                column: "CreatedDate")
                .Annotation("SqlServer:Include", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Wf_Workflow_OrderIndex",
                table: "Wf_Workflow",
                column: "OrderIndex")
                .Annotation("SqlServer:Include", new[] { "IsActive", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sys_LogApi");

            migrationBuilder.DropTable(
                name: "Sys_RefreshToken");

            migrationBuilder.DropTable(
                name: "Sys_RoleActivity");

            migrationBuilder.DropTable(
                name: "Sys_UserActivity");

            migrationBuilder.DropTable(
                name: "Sys_UserRole");

            migrationBuilder.DropTable(
                name: "Wf_AutoCondition");

            migrationBuilder.DropTable(
                name: "Wf_FieldConfig");

            migrationBuilder.DropTable(
                name: "Wf_NotificationAttachment");

            migrationBuilder.DropTable(
                name: "Wf_NotificationRecipient");

            migrationBuilder.DropTable(
                name: "Wf_Process");

            migrationBuilder.DropTable(
                name: "Wf_StatusFieldRule");

            migrationBuilder.DropTable(
                name: "Wf_UpdateMode");

            migrationBuilder.DropTable(
                name: "Sys_Activity");

            migrationBuilder.DropTable(
                name: "Sys_Account");

            migrationBuilder.DropTable(
                name: "Sys_Role");

            migrationBuilder.DropTable(
                name: "Wf_Field");

            migrationBuilder.DropTable(
                name: "Wf_TransitionNotification");

            migrationBuilder.DropTable(
                name: "Wf_StatusTransition");

            migrationBuilder.DropTable(
                name: "Wf_Status");

            migrationBuilder.DropTable(
                name: "Wf_Workflow");
        }
    }
}
