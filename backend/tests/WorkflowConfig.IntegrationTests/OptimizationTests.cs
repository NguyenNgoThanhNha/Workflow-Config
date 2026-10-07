using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowConfig.Application.Features.V1.Maintenance.Commands.PurgeExpiredData;
using WorkflowConfig.Domain.Entities.Sys;
using WorkflowConfig.Persistence;

namespace WorkflowConfig.IntegrationTests;

/// <summary>Dọn dữ liệu kỹ thuật (RULES 4.11) và nén response (RULES 8.8) — chạy trên SQL thật.</summary>
[Collection(ApiCollection.Name)]
public class OptimizationTests(WorkflowConfigApiFactory factory)
{
    private async Task<HttpClient> AdminAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = WorkflowConfigApiFactory.AdminEmail, password = WorkflowConfigApiFactory.AdminPassword });
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            JsonNode.Parse(await login.Content.ReadAsStringAsync())!["accessToken"]!.GetValue<string>());
        return client;
    }

    [Fact]
    public async Task Purge_deletes_only_expired_refresh_tokens()
    {
        var now = DateTime.UtcNow;
        Guid expired, revokedButValid, active;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkflowConfigDbContext>();
            var userId = await db.SysAccounts.Where(u => u.Email == WorkflowConfigApiFactory.AdminEmail).Select(u => u.Id).SingleAsync();
            SysRefreshToken Token(DateTime expiresAt, bool revoked) => new()
            {
                UserId = userId, TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = expiresAt, RevokedAt = revoked ? now.AddDays(-1) : null
            };
            var tokens = new[] { Token(now.AddDays(-3), false), Token(now.AddDays(5), true), Token(now.AddDays(5), false) };
            db.SysRefreshTokens.AddRange(tokens);
            await db.SaveChangesAsync();
            (expired, revokedButValid, active) = (tokens[0].Id, tokens[1].Id, tokens[2].Id);
        }

        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PurgeExpiredDataCommand());

        using (var scope = factory.Services.CreateScope())
        {
            var ids = await scope.ServiceProvider.GetRequiredService<WorkflowConfigDbContext>().SysRefreshTokens
                .IgnoreQueryFilters().Where(t => t.Id == expired || t.Id == revokedButValid || t.Id == active)
                .Select(t => t.Id).ToListAsync();
            Assert.DoesNotContain(expired, ids);
            Assert.Contains(revokedButValid, ids); // còn hạn → giữ để phát hiện token bị dùng lại
            Assert.Contains(active, ids);
        }
    }

    [Fact]
    public async Task Responses_are_compressed_except_auth_and_api_log_keeps_plain_json()
    {
        var admin = await AdminAsync();

        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users?pageSize=5");
        get.Headers.AcceptEncoding.ParseAdd("br");
        Assert.Equal("br", Assert.Single((await admin.SendAsync(get)).Content.Headers.ContentEncoding));

        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = WorkflowConfigApiFactory.AdminEmail, password = WorkflowConfigApiFactory.AdminPassword })
        };
        login.Headers.AcceptEncoding.ParseAdd("br, gzip");
        Assert.Empty((await factory.CreateClient().SendAsync(login)).Content.Headers.ContentEncoding); // có token → không nén

        var marker = $"cmp-{Guid.NewGuid():N}"[..30];
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/roles")
        {
            Content = JsonContent.Create(new { name = marker, description = "compression test", activities = Array.Empty<object>() })
        };
        create.Headers.AcceptEncoding.ParseAdd("br");
        var created = await admin.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("br", Assert.Single(created.Content.Headers.ContentEncoding));

        string? logged = null;
        for (var i = 0; i < 20 && logged is null; i++)
        {
            await Task.Delay(500);
            var logs = await admin.GetFromJsonAsync<JsonObject>("/api/v1/api-logs?url=/api/v1/roles&method=POST&pageSize=20");
            foreach (var item in logs!["items"]!.AsArray())
            {
                var detail = await admin.GetFromJsonAsync<JsonObject>($"/api/v1/api-logs/{item!["id"]}");
                if (detail!["response"]?.GetValue<string>() is { } body && body.Contains(marker)) { logged = body; break; }
            }
        }
        Assert.NotNull(logged); // log chứa JSON gốc, không phải byte đã nén
    }
}
