# WorkflowConfig — Backend

ASP.NET Core Web API (Clean Architecture + CQRS) cho ứng dụng cấu hình quy trình.

- 📐 **Luật bắt buộc:** [RULES.md](RULES.md)
- Nghiệp vụ: `src/WorkflowConfig.Application/Features/V1/Workflows` · entity `Domain/Entities/Workflows` (bảng `Wf_*`) · controller `Api/Controllers/V1/WorkflowsController.cs`
- Phân quyền 6 bảng `Sys_*`, activity `WORKFLOW` (C/R/U/D); role mặc định `User` có `WORKFLOW:R`.

## Chạy

```bash
dotnet run --project src/WorkflowConfig.Api
```

Swagger ở http://localhost:5094/swagger. Môi trường Development tự migrate và seed: activity, role Admin/User, danh mục quy trình, một workflow mẫu và tài khoản `admin@local.dev` (mật khẩu trong `appsettings.Development.json`).

```bash
dotnet test tests/WorkflowConfig.UnitTests
```

```bash
TEST_SQL_CONNECTION="Server=.\MSSQLSERVER01;Trusted_Connection=True;TrustServerCertificate=True" dotnet test tests/WorkflowConfig.IntegrationTests
```

(Không đặt `TEST_SQL_CONNECTION` thì integration test tự dùng Testcontainers, cần Docker.)

## API

```text
GET|POST /api/v1/workflows · GET|PUT /api/v1/workflows/{id}      [WORKFLOW:R/C/U]
POST /api/v1/workflows/{id}/copy                                 [WORKFLOW:C]
GET|PUT /api/v1/workflows/{id}/image                             [WORKFLOW:R/U]
GET  /api/v1/workflows/lookups | fields | crm-tables | crm-tables/{table}/columns   [WORKFLOW:R]
GET  /api/v1/workflows/{id}/diagram | transition-table          [WORKFLOW:R]
PUT  /api/v1/workflows/{id}/statuses/{statusId}/position · /branches/position       [WORKFLOW:U]
GET  /api/v1/workflows/{id}/statuses/form?statusId=              [WORKFLOW:R]
POST|PUT|DELETE /api/v1/workflows/{id}/statuses[/{statusId}]     [WORKFLOW:U/D]
GET|POST|PUT|DELETE /api/v1/workflows/{id}/transitions[/{transitionId}]   [WORKFLOW:R/U/D]

POST /api/v1/auth/register | login | refresh | logout | forgot-password | reset-password
GET  /api/v1/auth/me
GET  /api/v1/activities · /api/v1/roles · /api/v1/users · /api/v1/api-logs
GET  /health
```
