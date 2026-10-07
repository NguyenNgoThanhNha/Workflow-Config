using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace WorkflowConfig.IntegrationTests;

/// <summary>Bảng Kanban trên SQL Server thật: phân quyền, xếp trạng thái vào cột, index unique (bảng, trạng thái).</summary>
[Collection(ApiCollection.Name)]
public class KanbanApiTests(WorkflowConfigApiFactory factory)
{
    private Task<HttpClient> AdminAsync() =>
        factory.CreateClientForAsync(WorkflowConfigApiFactory.AdminEmail, WorkflowConfigApiFactory.AdminPassword);

    private async Task<HttpClient> ViewerAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"kb{Guid.NewGuid():N}@test.local", password = "Secret@123", fullName = "Viewer" });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            JsonNode.Parse(await response.Content.ReadAsStringAsync())!["accessToken"]!.GetValue<string>());
        return client;
    }

    [Fact]
    public async Task Board_round_trip_and_viewer_is_read_only()
    {
        var admin = await AdminAsync();
        var code = $"KB_{Guid.NewGuid():N}"[..16];
        var created = await admin.PostAsJsonAsync("/api/v1/kanbans", new
        {
            code, name = "Bảng test", orderIndex = 1,
            columns = new[] { new { name = "Cần làm", orderIndex = 1, color = "#42526E" }, new { name = "Xong", orderIndex = 2, color = (string?)null } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var kanban = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;
        var id = kanban["id"]!.GetValue<string>();
        var todo = kanban["columns"]![0]!["id"]!.GetValue<string>();

        // bảng có thẻ của workflow mẫu (seed), chưa xếp cột nào
        var board = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/kanbans/{id}/board"))!;
        var card = board["cards"]!.AsArray().First()!;
        Assert.Null(card["columnId"]?.GetValue<string>());
        var statusId = card["statusId"]!.GetValue<string>();

        // xếp hai lần vào hai cột → vẫn một dòng (index unique không lỗi)
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/v1/kanbans/{id}/mappings", new { statusId, columnId = todo })).StatusCode);
        var done = kanban["columns"]![1]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/v1/kanbans/{id}/mappings", new { statusId, columnId = done })).StatusCode);
        board = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/kanbans/{id}/board"))!;
        Assert.Equal(done, board["cards"]!.AsArray().Single(c => c!["statusId"]!.GetValue<string>() == statusId)!["columnId"]!.GetValue<string>());

        var list = (await admin.GetFromJsonAsync<JsonNode>($"/api/v1/kanbans?keyword={code}"))!;
        Assert.Equal(1, list["items"]![0]!["mappedStatusCount"]!.GetValue<int>());

        var viewer = await ViewerAsync();
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/v1/kanbans/{id}/board")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PutAsJsonAsync($"/api/v1/kanbans/{id}/mappings", new { statusId, columnId = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.DeleteAsync($"/api/v1/kanbans/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/kanbans/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/kanbans/{id}")).StatusCode);
    }
}
