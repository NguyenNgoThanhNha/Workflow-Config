# CLAUDE.md

Backend .NET dựng từ **WorkflowConfig** (Clean Architecture + CQRS + phân quyền 6 bảng).

**Trước khi viết hoặc sửa code, đọc và tuân thủ [RULES.md](RULES.md). Mọi mục [BẮT BUỘC] không có ngoại lệ.**

Tóm tắt những điều hay bị vi phạm:
- Handler inject `IUnitOfWork<WorkflowConfigDbContext>`, không inject DbContext; `SaveChangesAsync` một lần ở cuối.
- Feature đặt tại `Application/Features/V1/<Feature>/{Commands,Queries,DTOs}`; một Command/Query = một use case; Command luôn có Validator.
- Controller mỏng, mọi endpoint có `[HasPermission(ConstActivity.X, ActivityType.Y)]`; activity mới thêm vào `ConstActivity.All`.
- Ném exception chuẩn (`NotFoundException`, `ForbiddenException`...), không try/catch trả lỗi; không trả entity ra API.
- Không chuỗi mã nghiệp vụ trần; thời gian dùng `TimeProvider` (UTC).
- Field nhạy cảm mới → thêm vào `ApiLogging:SensitiveFields`.
- SP gọi bằng `ExecuteStoreProcedureGetMultiTablesAsync`; index trên bảng xóa mềm phải INCLUDE `IsDeleted`; tối ưu phải có số đo trước–sau.
- Bảng kỹ thuật tăng mãi → thêm vào `PurgeExpiredDataCommand`; số liệu tổng hợp dùng chung → `HybridCache`.
- Tìm chữ trong danh sách: dùng cột chuẩn hóa (`SearchNormalizer`, collation `Latin1_General_100_BIN2`) + `OPTION (RECOMPILE)`; điều kiện đắt tiền trong OR bọc `CASE` lồng nhau (RULES 3.13).
- Sửa file bằng Edit/Write hoặc Node. **Không** dùng `Get-Content`/`Set-Content` của PowerShell 5.1 để ghi lại file: nó phá UTF-8 tiếng Việt thành mojibake.

Lệnh thường dùng:

```bash
dotnet build WorkflowConfig.sln
dotnet test tests/WorkflowConfig.UnitTests
dotnet ef migrations add <Name> -p src/WorkflowConfig.Persistence -s src/WorkflowConfig.Persistence
dotnet run --project src/WorkflowConfig.Api
```

Debug lỗi: lấy `traceId` trong ProblemDetails → `GET /api/v1/api-logs?traceId=...` → tìm `traceId` trong `src/WorkflowConfig.Api/logs/*.log`.
