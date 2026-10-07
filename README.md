# Workflow-Config — Cấu hình quy trình

Port toàn bộ chức năng **WorkFlow** của MVC cũ (`VAS_CRM_BE/src/WorkflowWeb/SourceCode/ISD.Admin/Areas/Work`, `WorkFlowController` + views) sang:

- **backend/** — dựng từ `Backend_Api_Template` (đổi tên `ServerApiTemplate` → `WorkflowConfig`), theo [backend/RULES.md](backend/RULES.md): CQRS `Features/V1/Workflows`, `IUnitOfWork<>`, phân quyền 6 bảng (`WORKFLOW` C/R/U/D), log API, ProblemDetails.
- **frontend/** — React 19 + shadcn/ui, cấu trúc feature-based (`src/features/workflows/{api,hooks,components,pages,schemas.ts,types.ts,index.ts}`); sơ đồ dùng React Flow thay jsPlumb.

## Đối chiếu chức năng MVC → mới

| MVC cũ | Mới |
|---|---|
| `Index` + `_Search` (tìm mã/tên, lọc Actived) | `/workflows` — tìm không dấu, lọc, phân trang · `GET /api/v1/workflows` |
| `Copy` (modal, kiểm tra trùng mã/tên) | Nút Copy trên danh sách · `POST /workflows/{id}/copy` — nhân bản trạng thái, bước chuyển, điều kiện, thông báo (+Cc/Bcc/đính kèm), cấu hình field, quyền field theo trạng thái; **một** SaveChanges |
| `Create` / `Edit` (thông tin, ảnh, `_FormTaskStatus`, `_FormWorkFlowField`) | `/workflows/new`, `/workflows/{id}` · `POST/PUT /workflows`, `PUT /workflows/{id}/image` |
| `Workflow` (sơ đồ jsPlumb), `UpdatePositionTaskTransition`, `GetStatusTransition` | `/workflows/{id}/designer` tab **Sơ đồ** · `GET /diagram`, `PUT /statuses/{id}/position`, `PUT /branches/position` |
| `FindTaskStatus` / `UpdateTaskStatus` / `DeleteTaskStatus` (+ cấu hình Disable/Required field) | Nhấp đúp ô trạng thái · `GET /statuses/form`, `POST/PUT/DELETE /statuses` |
| `_WFCreateTransition` / `SaveTest` / `Save` / `DeleteStatusTransition` (+ ký số, auto condition, notification Zalo/Push/Email) | Kéo nối 2 ô hoặc nhấp đúp mũi tên · `GET/POST/PUT/DELETE /transitions` |
| `Config` (bảng trạng thái × bước chuyển) | Tab **Bảng bước chuyển** · `GET /transition-table` |
| `GetsTable` / `GetFiledOfTable` (ghép chuỗi SQL) | `GET /crm-tables`, `/crm-tables/{table}/columns` — dùng `SqlParameter`, chỉ schema trong `Workflow:NotificationTableSchemas` |
| Catalog `process`, `StatusTransition_UpdateModeModel`, `WorkFlowFieldModel` (nhập tay trên DB) | `Wf_Process`, `Wf_UpdateMode`, `Wf_Field` — seed từ code (`WorkflowSeed`) |

Khác biệt có chủ ý (sửa lỗi / làm chặt của bản cũ):
- Bỏ trạng thái đang có bước chuyển khỏi form → **409** rõ ràng (bản cũ lỗi FK). Sửa workflow có `rowVersion` → 2 người cùng sửa thì người sau nhận 409.
- Lưu bước chuyển: điều kiện / thông báo đồng bộ theo Id (bản cũ khi sửa chỉ xử lý Zalo & Push, bỏ sót Email).
- Không ký → tự bỏ Người ký; cách cập nhật assignee/reporter chỉ giữ giá trị khớp chế độ (Roles → nhóm, Department/Employee → mã).
- `StatusTransition_Reporter_Department_Mapping`, `Kanban_TaskStatus_Mapping`: form cũ không còn gửi dữ liệu / không có Kanban ở đây nên không port; giá trị phòng ban lưu ở `ReporterValue`.
- Cấu hình ngôn ngữ field (`WorkFlowConfigLanguageModel`) gộp vào cột `NoteEn` của `Wf_FieldConfig`.

## Chạy

```bash
dotnet run --project backend/src/WorkflowConfig.Api
```

```bash
npm --prefix frontend run dev
```

API http://localhost:5094/swagger (Development tự migrate + seed danh mục, một workflow mẫu và `admin@local.dev` — mật khẩu trong `appsettings.Development.json`). Web http://localhost:5176 (proxy `/api` sang 5094).

## Test

```bash
dotnet test backend/tests/WorkflowConfig.UnitTests
```

```bash
TEST_SQL_CONNECTION="Server=.\MSSQLSERVER01;Trusted_Connection=True;TrustServerCertificate=True" dotnet test backend/tests/WorkflowConfig.IntegrationTests
```

```bash
npm --prefix frontend test -- --run --pool=threads --maxWorkers=2
```

Hiện tại: BE 40 unit + 10 integration (SQL Server thật), FE 44.
