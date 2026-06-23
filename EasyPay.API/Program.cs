using Serilog;
using EasyPay.API.Extensions;
using EasyPay.API.Middleware;

// ── Bootstrap logger before host builds ───────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/easypay-.log",
            rollingInterval:    RollingInterval.Day,
            retainedFileCountLimit: 30));

    // ── Database ──────────────────────────────────────────
    builder.Services.AddDatabase(builder.Configuration);

    // ── Repositories + Services ───────────────────────────
    builder.Services.AddRepositories();
    builder.Services.AddApplicationServices();

    // ── JWT ───────────────────────────────────────────────
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // ── API Versioning ────────────────────────────────────
    builder.Services.AddApiVersioning(o =>
    {
        o.DefaultApiVersion                = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions                = true;
    });

    builder.Services.AddVersionedApiExplorer(o =>
    {
        o.GroupNameFormat           = "'v'VVV";
        o.SubstituteApiVersionInUrl = true;
    });

    // ── Controllers ───────────────────────────────────────
    builder.Services.AddControllers();

    // ── CORS ──────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("EasyPayCors", policy =>
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // ── Swagger ───────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerDocumentation();

    // ── Build app ─────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
        app.UseSwaggerDocumentation();

    app.UseHttpsRedirection();
    app.UseCors("EasyPayCors");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("EasyPay API starting on {Env}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EasyPay API failed to start.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
