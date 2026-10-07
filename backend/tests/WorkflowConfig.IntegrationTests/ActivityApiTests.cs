using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace WorkflowConfig.IntegrationTests;

/// <summary>Thêm chức năng trên giao diện: chỉ ai có quyền ROLE; chức năng hệ thống bị khóa; quyền role tôn trọng Actions.</summary>
[Collection(ApiCollection.Name)]
public class ActivityApiTests(WorkflowConfigApiFactory factory)
{
    [Fact]
    public async Task Custom_activity_round_trip()
    {
        var admin = await factory.CreateClientForAsync(WorkflowConfigApiFactory.AdminEmail, WorkflowConfigApiFactory.AdminPassword);
        var code = $"EXPORT_{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        var created = await admin.PostAsJsonAsync("/api/v1/activities", new { code, name = "Xuất file Excel", actions = "R" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = JsonNode.Parse(await created.Content.ReadAsStringAsync())!["id"]!.GetValue<string>();

        var all = (await admin.GetFromJsonAsync<JsonArray>("/api/v1/activities"))!;
        Assert.Contains(all, a => a!["code"]!.GetValue<string>() == code && !a["isSystem"]!.GetValue<bool>());
        var system = all.First(a => a!["code"]!.GetValue<string>() == "WORKFLOW")!;
        Assert.True(system["isSystem"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/activities/{system["id"]}")).StatusCode);

        // role chỉ được bật quyền chức năng có dùng
        var bad = await admin.PostAsJsonAsync("/api/v1/roles", new
        {
            name = $"Role {code}", activities = new[] { new { activityId = id, c = true, r = true, u = false, d = false } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var ok = await admin.PostAsJsonAsync("/api/v1/roles", new
        {
            name = $"Role {code}", activities = new[] { new { activityId = id, c = false, r = true, u = false, d = false } }
        });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

        // tài khoản thường không có quyền ROLE → 403
        var user = factory.CreateClient();
        var reg = await user.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"act{Guid.NewGuid():N}@test.local", password = "Secret@123", fullName = "U" });
        user.DefaultRequestHeaders.Authorization = new("Bearer",
            JsonNode.Parse(await reg.Content.ReadAsStringAsync())!["accessToken"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/v1/activities", new { code = "X_TEST", name = "x", actions = "R" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/activities/{id}")).StatusCode);
    }
}
