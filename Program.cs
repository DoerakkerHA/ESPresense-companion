using System;
using ESPresense;
using ESPresense.Models;
using Serilog;
using Serilog.Events;
using SQLite;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, cfg) => cfg.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SQLiteConnection>(a=>{
    var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".espresense/config.db");
    Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? throw new InvalidOperationException("HOME not found"));
    return new SQLiteConnection(databasePath);
});
builder.Services.AddHostedService<Multilateralizer>();
builder.Services.AddSingleton<State>();

var app = builder.Build();

app.UseSerilogRequestLogging(o =>
{
    o.EnrichDiagnosticContext = (dc, ctx) => dc.Set("UserAgent", ctx?.Request?.Headers["User-Agent"]);
    o.GetLevel = (ctx, ms, ex) => ex != null ? LogEventLevel.Error : ctx.Response.StatusCode > 499 ? LogEventLevel.Error : ms > 500 ? LogEventLevel.Warning : LogEventLevel.Debug;
});
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

var db = app.Services.GetRequiredService<SQLiteConnection>();
db.CreateTable<Node>();


app.Run();