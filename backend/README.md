# 🚀 WorkflowConfig (v2 — chuẩn thống nhất)

Template ASP.NET Core Web API dùng chung cho các dự án backend .NET. Bản v2 gộp các nguồn sau:
- Template cũ: phân quyền 6 bảng `Sys_*`, `BaseEntity` có audit và xóa mềm, UnitOfWork có helper gọi stored procedure, lớp `Const*`.
- Convention CQRS + `IUnitOfWork<TContext>`.
- Cách log API của VAS_CRM_BE.

Tất cả được sắp theo **Clean Architecture**.

- 📐 **Luật bắt buộc:** [RULES.md](RULES.md)
- 📘 **Giải thích chi tiết:** `Roadmap/projects/00-Chuan-Backend-DotNet.md`
- 🧪 **Ví dụ có nghiệp vụ thật:** `Projects/Helpdesk-Ticketing/backend`
- 🗃️ **Template cũ (v1)** nằm riêng trên nhánh **`v1-legacy`** (`git checkout v1-legacy`), để tham khảo các phần v2 chưa port (Microsoft Graph email, OneSignal, device, Azure Blob…). Nhánh `main` chỉ chứa v2.

## Có sẵn gì

| Nhóm | Nội dung |
|---|---|
| Kiến trúc | Domain / Persistence / Application / Infrastructure / Api · MediatR 12 (ValidationBehavior, LoggingBehavior) · FluentValidation · Mapster |
| Dữ liệu | EF Core + SQL Server · `IUnitOfWork<TContext>` + `IRepository<,>` (open generic, Scoped) · `ExecuteStoreProcedureGetMultiTables` + `ToDataSetSimpleRead().TryRead<T>()` · `BaseEntity` audit tự động + xóa mềm + global query filter · migration + `DesignTimeDbContextFactory` |
| Auth | JWT access token (15') + refresh token rotation (phát hiện dùng lại token) · quên/đặt lại mật khẩu · rate limit `auth/*` |
| Phân quyền | 6 bảng `Sys_Account, Sys_Role, Sys_Activity, Sys_UserRole, Sys_RoleActivity, Sys_UserActivity` · phân quyền **theo role và theo từng tài khoản** (C/R/U/D) · `[HasPermission]` policy động · cache có invalidate · API quản trị role/quyền đầy đủ |
| Log & debug | Serilog (console + file theo ngày, enrich UserId/TraceId) · **log API request/response vào `Sys_LogApi`** (ghi nền theo lô, che mật khẩu/token, tra theo `traceId`) · `LoggingDelegatingHandler` cho HttpClient |
| Lỗi | ProblemDetails (RFC 9457) có `traceId` · ánh xạ exception → 400/401/403/404/409/500 |
| Khác | Swagger + JWT · health check `/health` · CORS theo config · Dockerfile · JSON camelCase, enum string, DateTime UTC có `Z` |
| Test | Unit (xUnit + NSubstitute + `TestDb` InMemory) · Integration (`WebApplicationFactory` + Testcontainers hoặc SQL local) |

## Tạo dự án mới từ template

1. Copy repo (clone, hoặc tải ZIP của nhánh này).
2. Đổi tên: dùng PowerToys **PowerRename** trên thư mục, Search `WorkflowConfig` → Replace `TenDuAn` (áp dụng cho file và thư mục). Sau đó Find & Replace toàn solution nội dung `WorkflowConfig` → `TenDuAn`.
3. Xóa migration cũ và tạo lại:

   ```bash
   dotnet ef migrations add InitialCreate -p src/TenDuAn.Persistence -s src/TenDuAn.Persistence
   ```

4. Sửa `appsettings.Development.json` (connection string, `Seed:AdminEmail/AdminPassword`) và `ConstActivity.ApplicationName`.
5. Viết nghiệp vụ theo mục **"Thêm một feature mới"** trong [RULES.md](RULES.md).

## Chạy

```bash
dotnet run --project src/WorkflowConfig.Api
```

Swagger ở http://localhost:5080/swagger. Môi trường Development tự migrate và seed: activity, role Admin/User, và tài khoản `admin@local.dev` / `Admin@123`.

```bash
dotnet test tests/WorkflowConfig.UnitTests
```

```bash
TEST_SQL_CONNECTION="Server=.\MSSQLSERVER01;Trusted_Connection=True;TrustServerCertificate=True" dotnet test tests/WorkflowConfig.IntegrationTests
```

(Không đặt `TEST_SQL_CONNECTION` thì integration test tự dùng Testcontainers, cần Docker.)

## API có sẵn

```text
POST /api/v1/auth/register | login | refresh | logout | forgot-password | reset-password
GET  /api/v1/auth/me                                   → user + roles + quyền hiệu lực (cho FE ẩn/hiện)
GET  /api/v1/activities                                [ROLE:R | USER:R]
GET|POST /api/v1/roles · GET|PUT|DELETE /api/v1/roles/{id}         [ROLE:*]
GET  /api/v1/users · PATCH /api/v1/users/{id}          [USER:R/U]
PUT  /api/v1/users/{id}/roles                          [USER:U]
GET|PUT /api/v1/users/{id}/permissions                 [USER:R/U]  (quyền riêng từng tài khoản)
GET  /api/v1/api-logs · /api/v1/api-logs/{id}          [API_LOG:R] (debug)
GET  /health
```
