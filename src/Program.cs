using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Data;
using ShogaiBoard.Models;

// 対象システムマスターCSVインポートでShift_JIS（Excel由来のCSVで多い）を扱えるようにする。
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// SQLiteデータベースへの接続を登録する。
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Nginx等のリバースプロキシ配下でKestrelを動かす場合、X-Forwarded-For/Protoヘッダーから
// クライアントの実IP・スキームを復元する（リバースプロキシを使わない直接公開時は影響しない）。
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// 起動時に未適用のマイグレーションを自動適用し、DBファイルを最新状態にする。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// 現在発生中（未復旧）の障害一覧を返すAPI。Slack/Teams等の社内ツール連携用。
app.MapGet("/api/incidents", async (AppDbContext db) =>
{
    var incidents = await db.Incidents
        .Include(i => i.System)
        .Where(i => i.Status != IncidentStatus.Resolved)
        .OrderBy(i => i.Severity)
        .ThenBy(i => i.OccurredAt)
        .Select(i => new
        {
            system = i.System!.Name,
            description = i.Description,
            severity = i.Severity.ToString(),
            status = i.Status.ToString(),
            affectedScope = i.AffectedScope,
            occurredAt = i.OccurredAt,
            estimatedRecoveryAt = i.EstimatedRecoveryAt
        })
        .ToListAsync();

    return Results.Ok(incidents);
});

app.Run();
