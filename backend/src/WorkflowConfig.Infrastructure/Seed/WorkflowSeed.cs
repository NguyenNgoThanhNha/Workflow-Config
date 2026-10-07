using Microsoft.EntityFrameworkCore;
using WorkflowConfig.Application.Common.Interfaces;
using WorkflowConfig.Domain.Constants;
using WorkflowConfig.Domain.Entities.Workflows;
using WorkflowConfig.Persistence;

namespace WorkflowConfig.Infrastructure.Seed;

/// <summary>
/// Danh mục của module cấu hình quy trình, đồng bộ từ code mỗi lần khởi động — chỉ thêm mã còn thiếu,
/// không ghi đè tên/màu admin đã sửa.
/// </summary>
internal static class WorkflowSeed
{
    private const string DemoWorkflowCode = "DEMO_DUYET_HD";

    private static readonly (string Code, string Name, string Background, string Text)[] Processes =
    [
        (ConstWorkflow.Process.Todo, "Cần làm", "#DFE1E6", "#42526E"),
        (ConstWorkflow.Process.Processing, "Đang xử lý", "#DEEBFF", "#0747A6"),
        (ConstWorkflow.Process.Completed, "Hoàn thành", "#E3FCEF", "#006644"),
        (ConstWorkflow.Process.Unmapped, "Chưa phân loại", "#FFFAE6", "#974F0C")
    ];

    private static readonly (string Code, string Name)[] UpdateModes =
    [
        (ConstWorkflow.UpdateMode.NotConfig, "Không cấu hình"),
        (ConstWorkflow.UpdateMode.CreateUser, "Người tạo"),
        (ConstWorkflow.UpdateMode.SubmitUser, "Người nhấn nút"),
        (ConstWorkflow.UpdateMode.Roles, "Chọn nhóm"),
        (ConstWorkflow.UpdateMode.Remove, "Xóa trống"),
        (ConstWorkflow.UpdateMode.Department, "Phòng ban"),
        (ConstWorkflow.UpdateMode.Employee, "Chọn nhân viên")
    ];

    /// <summary>Các trường của form nhiệm vụ (theo thứ tự trên form).</summary>
    private static readonly (string Code, string Name)[] Fields =
    [
        ("WorkFlowId", "Loại nhiệm vụ"), ("TaskStatusId", "Trạng thái"), ("Summary", "Tiêu đề"),
        ("Description", "Mô tả"), ("Requirement", "Yêu cầu"), ("ProfileId", "Khách hàng"),
        ("PriorityCode", "Mức độ ưu tiên"), ("ReceiveDate", "Ngày tiếp nhận"), ("StartDate", "Ngày bắt đầu"),
        ("EstimateEndDate", "Ngày kết thúc dự kiến"), ("EndDate", "Ngày kết thúc"), ("Assignee", "Người được phân công"),
        ("RoleName", "Nhóm được phân công"), ("IsAssignGroup", "Phân công theo nhóm"), ("Reporter", "Người theo dõi/giám sát"),
        ("ReporterRoleName", "Nhóm theo dõi"), ("SaleEmployeeCode", "Nhân viên kinh doanh"), ("SalesSupervisorCode", "Giám sát bán hàng"),
        ("StoreId", "Chi nhánh"), ("ShowroomCode", "Showroom"), ("VisitTypeCode", "Loại ghé thăm"),
        ("VisitDate", "Ngày ghé thăm"), ("VisitPlace", "Nơi ghé thăm"), ("VisitAddress", "Địa chỉ ghé thăm"),
        ("VisitSaleOfficeCode", "Khu vực"), ("ProvinceId", "Tỉnh/Thành phố"), ("DistrictId", "Quận/Huyện"),
        ("WardId", "Phường/Xã"), ("ChannelCode", "Kênh"), ("CustomerClassCode", "Phân loại khách hàng"),
        ("CustomerSatisfactionCode", "Mức độ hài lòng"), ("CustomerReviews", "Đánh giá của khách hàng"), ("HasRequest", "Yêu cầu"),
        ("ServiceTechnicalTeamCode", "Tổ dịch vụ kỹ thuật"), ("ConstructionUnit", "Đơn vị thi công"), ("ProductCategoryCode", "Nhóm sản phẩm"),
        ("ProductWarrantyId", "Bảo hành sản phẩm"), ("HouseType", "Loại nhà"), ("ShapeType", "Hình dạng"),
        ("VoucherType", "Loại voucher"), ("VoucherDetail", "Chi tiết voucher"), ("SaleEmployeeOffer", "Đề xuất của NVKD"),
        ("HasShelf", "Có kệ trưng bày"), ("TextSponsor", "Nội dung tài trợ"), ("AmountSponsor", "Số tiền tài trợ"),
        ("Reviews", "Nhận xét"), ("Ratings", "Xếp hạng"), ("isRequiredCheckin", "Bắt buộc check-in"),
        ("CheckInTime", "Thời gian check-in"), ("isVisitCabinetPro", "Ghé thăm Cabinet Pro"), ("IsTogether", "Đi cùng"),
        ("isRemind", "Nhắc nhở"), ("isPrivate", "Riêng tư"), ("FileUrl", "File đính kèm"),
        ("Date1", "Ngày 1"), ("Text1", "Văn bản 1"), ("Text3", "Văn bản 3"),
        ("Property1", "Thuộc tính 1"), ("Property2", "Thuộc tính 2"), ("Property4", "Thuộc tính 4"),
        ("Property5", "Thuộc tính 5"), ("Property6", "Thuộc tính 6"), ("Tab_FileUrl", "Tab file đính kèm"),
        ("Tab_Product", "Tab sản phẩm"), ("Tab_Catalogue", "Tab catalogue"), ("Tab_Survey", "Tab khảo sát")
    ];

    public static async Task SyncCatalogsAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, CancellationToken ct)
    {
        var processes = unitOfWork.Repository<WorkflowProcess>();
        var existingProcesses = await processes.Select(p => p.Code).ToListAsync(ct);
        foreach (var (item, index) in Processes.Select((p, i) => (p, i)).Where(x => !existingProcesses.Contains(x.p.Code)))
        {
            processes.Add(new WorkflowProcess
            {
                Code = item.Code, Name = item.Name, BackgroundColor = item.Background, TextColor = item.Text, OrderIndex = index + 1
            });
        }

        var modes = unitOfWork.Repository<TransitionUpdateMode>();
        var existingModes = await modes.Select(m => m.Code).ToListAsync(ct);
        foreach (var (item, index) in UpdateModes.Select((m, i) => (m, i)).Where(x => !existingModes.Contains(x.m.Code)))
        {
            modes.Add(new TransitionUpdateMode { Code = item.Code, Name = item.Name, OrderIndex = index + 1 });
        }

        var fields = unitOfWork.Repository<WorkflowField>();
        var existingFields = await fields.Select(f => f.Code).ToListAsync(ct);
        foreach (var (item, index) in Fields.Select((f, i) => (f, i)).Where(x => !existingFields.Contains(x.f.Code)))
        {
            fields.Add(new WorkflowField { Code = item.Code, Name = item.Name, OrderIndex = index + 1 });
        }
    }

    /// <summary>Một workflow mẫu có rẽ nhánh để màn sơ đồ có dữ liệu ngay ở môi trường dev.</summary>
    public static async Task SeedDemoWorkflowAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, CancellationToken ct)
    {
        if (await unitOfWork.Repository<Workflow>().AnyAsync(w => w.Code == DemoWorkflowCode, ct)) return;

        var workflow = new Workflow
        {
            Code = DemoWorkflowCode, Name = "Duyệt hợp đồng (mẫu)", CategoryCode = "NV", CompanyCode = "1000", OrderIndex = 1
        };
        workflow.RefreshSearchText();

        WorkflowStatus Status(string code, string name, int order, string process, int x, int y) =>
            new() { WorkflowId = workflow.Id, Code = code, Name = name, OrderIndex = order, ProcessCode = process, PositionX = x, PositionY = y };

        var draft = Status("NEW", "Mới tạo", 1, ConstWorkflow.Process.Todo, 40, 160);
        var review = Status("REVIEW", "Chờ duyệt", 2, ConstWorkflow.Process.Processing, 300, 160);
        var approved = Status("APPROVED", "Đã duyệt", 3, ConstWorkflow.Process.Completed, 620, 60);
        var rejected = Status("REJECTED", "Từ chối", 4, ConstWorkflow.Process.Completed, 620, 280);
        foreach (var s in new[] { draft, review, approved, rejected }) workflow.Statuses.Add(s);

        StatusTransition Transition(string name, WorkflowStatus from, WorkflowStatus to, string outAnchor, string inAnchor, string? branch = null)
        {
            var t = new StatusTransition
            {
                WorkflowId = workflow.Id, Name = name, FromStatusId = from.Id, ToStatusId = to.Id,
                SourceAnchor = outAnchor, TargetAnchor = inAnchor, IsAssigneeAllowed = true,
                AssigneeUpdateMode = ConstWorkflow.UpdateMode.NotConfig, ReporterUpdateMode = ConstWorkflow.UpdateMode.NotConfig,
                SignatureType = ConstWorkflow.Signature.None
            };
            t.SetBranch(branch);
            if (branch is not null) { t.BranchPositionX = 470; t.BranchPositionY = 160; }
            return t;
        }

        workflow.Transitions.Add(Transition("Gửi duyệt", draft, review, ConstWorkflow.Anchor.Right, ConstWorkflow.Anchor.Left));
        workflow.Transitions.Add(Transition("Duyệt", review, approved, ConstWorkflow.Anchor.Right, ConstWorkflow.Anchor.Left, "Kết quả duyệt"));
        workflow.Transitions.Add(Transition("Từ chối", review, rejected, ConstWorkflow.Anchor.Right, ConstWorkflow.Anchor.Left, "Kết quả duyệt"));

        foreach (var (code, order) in new[] { ("Summary", 1), ("Description", 2), ("Assignee", 3), ("Reporter", 4) })
        {
            workflow.FieldConfigs.Add(new WorkflowFieldConfig { WorkflowId = workflow.Id, FieldCode = code, OrderIndex = order, IsRequired = code == "Summary" });
        }

        unitOfWork.Repository<Workflow>().Add(workflow);
    }

    /// <summary>Bảng Kanban mẫu: 3 cột theo nhóm xử lý, xếp sẵn trạng thái của workflow mẫu (nếu có).</summary>
    public static async Task SeedDemoKanbanAsync(IUnitOfWork<WorkflowConfigDbContext> unitOfWork, CancellationToken ct)
    {
        const string code = "KB_CHUNG";
        if (await unitOfWork.Repository<Kanban>().AnyAsync(k => k.Code == code, ct)) return;

        var kanban = new Kanban { Code = code, Name = "Kanban chung (mẫu)", OrderIndex = 1 };
        kanban.RefreshSearchText();
        var columns = new Dictionary<string, KanbanColumn>
        {
            [ConstWorkflow.Process.Todo] = new() { KanbanId = kanban.Id, Name = "Cần làm", OrderIndex = 1, Color = "#42526E" },
            [ConstWorkflow.Process.Processing] = new() { KanbanId = kanban.Id, Name = "Đang xử lý", OrderIndex = 2, Color = "#0747A6" },
            [ConstWorkflow.Process.Completed] = new() { KanbanId = kanban.Id, Name = "Hoàn thành", OrderIndex = 3, Color = "#006644" }
        };
        foreach (var c in columns.Values) kanban.Columns.Add(c);
        unitOfWork.Repository<Kanban>().Add(kanban);

        // trạng thái của workflow mẫu: vừa thêm trong lần seed này (Local) hoặc đã có trong DB
        var demo = unitOfWork.Repository<Workflow>().Local.FirstOrDefault(w => w.Code == DemoWorkflowCode)
                   ?? await unitOfWork.Repository<Workflow>().FirstOrDefaultAsync(w => w.Code == DemoWorkflowCode, ct);
        if (demo is null) return;
        var statuses = demo.Statuses.Count > 0
            ? demo.Statuses.ToList()
            : await unitOfWork.Repository<WorkflowStatus>().Where(s => s.WorkflowId == demo.Id).ToListAsync(ct);
        foreach (var status in statuses.Where(s => s.Code != "REJECTED" && columns.ContainsKey(s.ProcessCode)))
        {
            unitOfWork.Repository<KanbanStatusMapping>().Add(new KanbanStatusMapping
            {
                KanbanId = kanban.Id, ColumnId = columns[status.ProcessCode].Id, StatusId = status.Id
            });
        }
    }
}
