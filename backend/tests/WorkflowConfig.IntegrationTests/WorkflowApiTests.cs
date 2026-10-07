using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace WorkflowConfig.IntegrationTests;

/// <summary>Module cấu hình quy trình chạy trên SQL Server thật: phân quyền, luồng tạo → bước chuyển → sơ đồ → copy, 409.</summary>
[Collection(ApiCollection.Name)]
public class WorkflowApiTests(WorkflowConfigApiFactory factory)
{
    private Task<HttpClient> AdminAsync() =>
        factory.CreateClientForAsync(WorkflowConfigApiFactory.AdminEmail, WorkflowConfigApiFactory.AdminPassword);

    /// <summary>Tài khoản tự đăng ký nhận role mặc định "User" = WORKFLOW:R.</summary>
    private async Task<HttpClient> ViewerAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"wf{Guid.NewGuid():N}@test.local", password = "Secret@123", fullName = "Viewer" });
        response.EnsureSuccessStatusCode();
        var token = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["accessToken"]!.GetValue<string>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static object NewWorkflow(string code) => new
    {
        code,
        name = $"Quy trình {code}",
        categoryCode = "NV",
        companyCode = "1000",
        orderIndex = 1,
        isActive = true,
        isSummaryDisabled = false,
        statuses = new[]
        {
            new { id = (Guid?)null, code = "NEW", name = "Mới tạo", orderIndex = 1, category = (string?)null, processCode = "todo" },
            new { id = (Guid?)null, code = "DONE", name = "Hoàn thành", orderIndex = 2, category = (string?)null, processCode = "completed" }
        },
        fields = new[]
        {
            new { fieldCode = "Summary", isRequired = true, orderIndex = 1, parameters = (string?)null, note = "Tiêu đề", noteEn = "Title",
                  hideWhenAdd = false, addDefaultValue = (string?)null, hideWhenEdit = false, editDefaultValue = (string?)null }
        },
        rowVersion = (string?)null
    };

    [Fact]
    public async Task Viewer_can_read_but_not_create_or_change_workflows()
    {
        var viewer = await ViewerAsync();

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/v1/workflows")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/v1/workflows", NewWorkflow("VIEWER_X"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync($"/api/v1/workflows/{Guid.NewGuid()}/copy", new { code = "C", name = "C", orderIndex = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.DeleteAsync($"/api/v1/workflows/{Guid.NewGuid()}/transitions/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Create_configure_diagram_copy_and_concurrency_round_trip()
    {
        var admin = await AdminAsync();
        var code = $"IT_{Guid.NewGuid():N}"[..20];

        // 1. Tạo workflow
        var created = await admin.PostAsJsonAsync("/api/v1/workflows", NewWorkflow(code));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var detail = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;
        var id = detail["id"]!.GetValue<string>();
        var statuses = detail["statuses"]!.AsArray();
        var from = statuses.First(s => s!["code"]!.GetValue<string>() == "NEW")!["id"]!.GetValue<string>();
        var to = statuses.First(s => s!["code"]!.GetValue<string>() == "DONE")!["id"]!.GetValue<string>();

        // 2. Mã trùng → 400 từ validator
        var duplicate = await admin.PostAsJsonAsync("/api/v1/workflows", NewWorkflow(code));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        // 3. Nguồn dữ liệu Zalo: metadata đọc INFORMATION_SCHEMA bằng tham số
        var tables = await admin.GetFromJsonAsync<string[]>("/api/v1/workflows/crm-tables");
        Assert.Contains("Wf_Workflow", tables!);
        var columns = await admin.GetFromJsonAsync<string[]>("/api/v1/workflows/crm-tables/Wf_Workflow/columns");
        Assert.Contains("Name", columns!);

        // 4. Bước chuyển có điều kiện tự động + thông báo Zalo (bảng tùy chọn) + email Cc
        var transition = await admin.PostAsJsonAsync($"/api/v1/workflows/{id}/transitions", new
        {
            name = "Hoàn tất",
            fromStatusId = from,
            toStatusId = to,
            sourceAnchor = "Right",
            targetAnchor = "Left",
            isAssigneeAllowed = true,
            isAutomatic = true,
            assigneeUpdateMode = "Department",
            assigneeValue = "PB01",
            signatureType = "INITIAL",
            signerType = "UNIT",
            conditions = new[] { new { conditionType = "FIELD", field = "Summary", comparisonType = "=", value = "'x'" } },
            notifications = new object[]
            {
                new { type = "Zalo", znsTemplateId = "123", useDefaultZaloData = false, crmTable = "Wf_Workflow", crmField = "Name" },
                new { type = "EMAIL", cc = new[] { new { mode = "Roles", configValue = "abc" } }, attachments = new[] { new { attachment = "a.pdf" } } }
            }
        });
        Assert.Equal(HttpStatusCode.Created, transition.StatusCode);
        var transitionId = (await transition.Content.ReadFromJsonAsync<Guid>()).ToString();
        var saved = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/workflows/{id}/transitions/{transitionId}"))!;
        Assert.Equal("Summary='x'", saved["conditions"]![0]!["sqlText"]!.GetValue<string>());
        Assert.Equal("dbo", saved["notifications"]![0]!["crmSchema"]!.GetValue<string>());
        Assert.Single(saved["notifications"]![1]!["cc"]!.AsArray());

        // 5. Cột không thuộc bảng → 400
        var badColumn = await admin.PutAsJsonAsync($"/api/v1/workflows/{id}/transitions/{transitionId}", new
        {
            name = "Hoàn tất", fromStatusId = from, toStatusId = to, signatureType = "NONE",
            notifications = new[] { new { type = "Zalo", znsTemplateId = "1", useDefaultZaloData = false, crmTable = "Wf_Workflow", crmField = "KhongCo" } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, badColumn.StatusCode);

        // 6. Sơ đồ có mũi tên vừa tạo; xóa trạng thái đang dùng → 409
        var diagram = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/workflows/{id}/diagram"))!;
        Assert.Contains(diagram["edges"]!.AsArray(), e => e!["label"]!.GetValue<string>() == "Hoàn tất (PB01)");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/workflows/{id}/statuses/{from}")).StatusCode);

        // 7. Copy nhân bản toàn bộ
        var copy = await admin.PostAsJsonAsync($"/api/v1/workflows/{id}/copy", new { code = code + "_C", name = $"Bản copy {code}", orderIndex = 2 });
        Assert.Equal(HttpStatusCode.Created, copy.StatusCode);
        var copyId = JsonNode.Parse(await copy.Content.ReadAsStringAsync())!["id"]!.GetValue<string>();
        var copyDiagram = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/workflows/{copyId}/diagram"))!;
        Assert.Equal(2, copyDiagram["statuses"]!.AsArray().Count);
        Assert.Single(copyDiagram["edges"]!.AsArray());

        // 8. Hai người cùng sửa: người thứ hai gửi rowVersion cũ → 409
        var current = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/workflows/{id}"))!;
        var update = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(NewWorkflow(code)))!;
        update["statuses"] = JsonNode.Parse(current["statuses"]!.ToJsonString());
        update["rowVersion"] = current["rowVersion"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/workflows/{id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/v1/workflows/{id}", update)).StatusCode);
    }
}
