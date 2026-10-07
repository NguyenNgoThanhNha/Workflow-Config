# Workflow-Config — Cấu hình quy trình

Ứng dụng cấu hình quy trình xử lý nhiệm vụ: trạng thái, bước chuyển, phân quyền, điều kiện tự động và thông báo.

- **backend/** — ASP.NET Core dựng từ `Backend_Api_Template`, theo [backend/RULES.md](backend/RULES.md): CQRS `Features/V1/Workflows`, `IUnitOfWork<>`, phân quyền 6 bảng (activity `WORKFLOW` C/R/U/D), log API, ProblemDetails.
- **frontend/** — React 19 + shadcn/ui, cấu trúc feature-based (`src/features/workflows/{api,hooks,components,pages,schemas.ts,types.ts,index.ts}`); sơ đồ dùng React Flow.

## Chức năng

| Màn hình | API |
|---|---|
| Danh sách workflow: tìm mã/tên (không dấu), lọc trạng thái sử dụng, phân trang | `GET /api/v1/workflows` |
| Copy workflow — nhân bản trạng thái, bước chuyển, điều kiện, thông báo, cấu hình field và quyền field theo trạng thái | `POST /workflows/{id}/copy` |
| Tạo / sửa workflow: thông tin chung, ảnh đại diện, trạng thái, cấu hình thuộc tính | `POST/PUT /workflows`, `PUT /workflows/{id}/image` |
| Sơ đồ: kéo thả trạng thái, kéo nối để tạo bước chuyển, nút rẽ nhánh | `GET /diagram`, `PUT /statuses/{id}/position`, `PUT /branches/position` |
| Trạng thái: màu, push notification, quyền sửa field theo vai trò | `GET /statuses/form`, `POST/PUT/DELETE /statuses` |
| Bước chuyển: ký số, phân quyền, cập nhật người phụ trách, điều kiện tự động, thông báo Push/Zalo/Email | `GET/POST/PUT/DELETE /transitions` |
| Bảng bước chuyển | `GET /transition-table` |
| Nguồn dữ liệu Zalo (bảng / cột) | `GET /crm-tables`, `/crm-tables/{table}/columns` — chỉ schema trong `Workflow:NotificationTableSchemas` |

Danh mục nhóm xử lý (`Wf_Process`), cách cập nhật người phụ trách (`Wf_UpdateMode`) và danh mục field (`Wf_Field`) được seed từ code (`WorkflowSeed`).

Quy tắc chính:
- Không xóa được trạng thái còn bước chuyển đi ra / đi vào (409).
- Sửa workflow kèm `rowVersion`: hai người cùng sửa thì người lưu sau nhận 409.
- Điều kiện / thông báo của bước chuyển đồng bộ theo Id (dòng bị bỏ khỏi form sẽ bị xóa).
- Không ký → bỏ Người ký; cách cập nhật người phụ trách chỉ giữ giá trị khớp chế độ (Roles → nhóm, Department/Employee → mã).

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
