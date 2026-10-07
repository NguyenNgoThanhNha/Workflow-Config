using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace WorkflowConfig.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ApiTests(WorkflowConfigApiFactory factory)
{
    private async Task<(HttpClient Client, JsonNode Auth)> RegisterAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"it{Guid.NewGuid():N}@test.local", password = "Secret@123", fullName = "IT user" });
        response.EnsureSuccessStatusCode();
        var auth = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth["accessToken"]!.GetValue<string>());
        return (client, auth);
    }

    [Fact]
    public async Task Admin_login_returns_isAdmin_and_all_permissions()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { email = WorkflowConfigApiFactory.AdminEmail, password = WorkflowConfigApiFactory.AdminPassword });

        response.EnsureSuccessStatusCode();
        var user = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["user"]!;
        Assert.True(user["isAdmin"]!.GetValue<bool>());
        Assert.Contains(user["permissions"]!.AsArray(), p => p!["code"]!.GetValue<string>() == "ROLE" && p["d"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Wrong_password_returns_401_problem_details()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { email = WorkflowConfigApiFactory.AdminEmail, password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Validation_error_returns_400_with_camelCase_field_errors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "weak", fullName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["errors"]!;
        Assert.NotNull(errors["email"]);
        Assert.NotNull(errors["password"]);
        Assert.NotNull(errors["fullName"]);
    }

    [Fact]
    public async Task New_user_is_forbidden_until_granted_per_account_permission()
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/v1/auth/login",
            new { email = WorkflowConfigApiFactory.AdminEmail, password = WorkflowConfigApiFactory.AdminPassword });
        admin.DefaultRequestHeaders.Authorization = new("Bearer",
            JsonNode.Parse(await login.Content.ReadAsStringAsync())!["accessToken"]!.GetValue<string>());
        var (user, auth) = await RegisterAsync();

        var denied = await user.GetAsync("/api/v1/roles");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var traceId = JsonNode.Parse(await denied.Content.ReadAsStringAsync())!["traceId"]!.GetValue<string>();

        var activities = await admin.GetFromJsonAsync<JsonArray>("/api/v1/activities");
        var roleActivityId = activities!.First(a => a!["code"]!.GetValue<string>() == "ROLE")!["id"]!.GetValue<string>();
        var grant = await admin.PutAsJsonAsync($"/api/v1/users/{auth["user"]!["id"]}/permissions",
            new { activities = new[] { new { activityId = roleActivityId, c = false, r = true, u = false, d = false } } });
        grant.EnsureSuccessStatusCode();

        // Quyền riêng có hiệu lực ngay, không cần đăng nhập lại.
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/v1/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync($"/api/v1/roles/{Guid.NewGuid()}")).StatusCode);

        // Log API: tra được request 403 theo traceId; mật khẩu lúc đăng ký bị che.
        JsonObject? logs = null;
        for (var i = 0; i < 20 && (logs?["totalCount"]?.GetValue<int>() ?? 0) == 0; i++)
        {
            await Task.Delay(500);
            logs = await admin.GetFromJsonAsync<JsonObject>($"/api/v1/api-logs?traceId={Uri.EscapeDataString(traceId)}");
        }
        Assert.Equal(403, logs!["items"]![0]!["statusCode"]!.GetValue<int>());

        var registerLog = await admin.GetFromJsonAsync<JsonObject>("/api/v1/api-logs?url=/auth/register&statusCode=200&pageSize=1");
        var detail = await admin.GetFromJsonAsync<JsonObject>($"/api/v1/api-logs/{registerLog!["items"]![0]!["id"]}");
        Assert.DoesNotContain("Secret@123", detail!["request"]!.GetValue<string>());
    }

    [Fact]
    public async Task Refresh_token_rotation_detects_reuse()
    {
        var (_, auth) = await RegisterAsync();
        var client = factory.CreateClient();
        var original = auth["refreshToken"]!.GetValue<string>();

        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var newToken = JsonNode.Parse(await rotated.Content.ReadAsStringAsync())!["refreshToken"]!.GetValue<string>();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = newToken })).StatusCode);
    }

    [Fact]
    public async Task Dates_are_serialized_as_utc_with_Z()
    {
        var admin = factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/v1/auth/login",
            new { email = WorkflowConfigApiFactory.AdminEmail, password = WorkflowConfigApiFactory.AdminPassword });
        admin.DefaultRequestHeaders.Authorization = new("Bearer",
            JsonNode.Parse(await login.Content.ReadAsStringAsync())!["accessToken"]!.GetValue<string>());

        var users = await admin.GetFromJsonAsync<JsonObject>("/api/v1/users?pageSize=1");
        Assert.EndsWith("Z", users!["items"]![0]!["createdDate"]!.GetValue<string>());
    }
}
