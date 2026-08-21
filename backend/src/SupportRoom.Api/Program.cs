using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SupportRoom.Api;
using SupportRoom.Api.Configurations;
using SupportRoom.Api.Hubs;
using SupportRoom.Application.Common;
using SupportRoom.Domain.Configuration;
using SupportRoom.Domain.Enums;
using SupportRoom.Infrastructure.Cors;
using SupportRoom.Infrastructure.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// Load real provider credentials from a gitignored .env (Development convenience) BEFORE anything
// reads them - every provider category is a hard requirement now (see ProviderSelectionReader),
// and the credential readers in ExternalServiceEnv read env lazily per request. Without this the
// only way to run at all was to export every variable by hand in the shell first. Existing
// shell/launchSettings variables always win (DotEnv never overrides).
if (builder.Environment.IsDevelopment())
{
    DotEnv.Load(Path.Combine(builder.Environment.ContentRootPath, ".env"));
}

// Console stays human-readable for `dotnet run`; the file sink mirrors that same plain-text
// format (rather than JSON) so CS/ops can open logs/*.log directly - CorrelationId is kept
// in every line so a failed provider call can still be traced across the request log,
// provider-call logs, and error log by grepping one id. Reads "Serilog:MinimumLevel" from
// appsettings so Development can be noisier than Production.
const string LogOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{CorrelationId}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: LogOutputTemplate)
    // Cap disk usage: one file per day, but also roll to a new file once a single day's log
    // passes 50 MB, and keep at most 14 files - otherwise logs grow unbounded and can fill the
    // disk on a busy day. shared:true so a second process (e.g. `dotnet ef`) can write too.
    .WriteTo.File(
        "logs/supportroom-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 50L * 1024 * 1024,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 14,
        shared: true,
        outputTemplate: LogOutputTemplate));

// Add services to the container.

// PropertyNamingPolicy = CamelCase must match ApiJsonOptions.Default exactly - the frontend
// (src/lib/api-client.ts) expects camelCase field names with zero changes on its side.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = ApiJsonOptions.Default.PropertyNamingPolicy;
}).ConfigureApiBehaviorOptions(options =>
{
    // [ApiController]'s automatic [Required]/[StringLength]/etc. validation runs before a
    // controller action (and its GeneralException handling) ever executes, so without this it
    // falls through to ASP.NET's default ValidationProblemDetails shape - {type,title,errors}
    // instead of this API's {error:{code,message,requestId}} envelope every other 4xx uses. The
    // frontend's ApiClientError only reads body.error.message, so callers silently lost the
    // real validation reason (e.g. POST /api/tts with empty text) and fell back to a generic
    // failure message instead.
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(msg => !string.IsNullOrWhiteSpace(msg))
            ?? "ข้อมูลที่ส่งมาไม่ถูกต้อง";
        return new BadRequestObjectResult(ApiErrorEnvelope.Build(ApiErrorCode.ValidationError, message));
    };
});

builder.Services.AddFrontendCors(builder.Environment.IsDevelopment());
builder.Services.AddEntityFrameworkConfiguration(builder.Configuration);
builder.Services.AddServiceConfiguration();
builder.Services.AddBackOfficeAuthentication();
builder.Services.AddLoginRateLimiting();
builder.Services.AddSignalR();
MapsterConfig.Apply();

// HttpStatusCodeExceptionHandler (GeneralException from a Service) runs first; anything it
// doesn't recognize falls through to GlobalExceptionHandler's generic 500 envelope.
builder.Services.AddExceptionHandler<HttpStatusCodeExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// TraceIdentifier is unique per-request and free (ASP.NET Core assigns it already) - pushing it
// into every log line written during this request (request log, provider-call logs, error log)
// is what makes `grep <id> logs/*.log` reconstruct one request's whole story.
app.Use(async (context, next) =>
{
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
    {
        await next();
    }
});

// Request logging sits OUTSIDE the exception handler so it records the final, client-visible
// status. Do not use Serilog's default request middleware here: its RequestPath property contains
// the public token on learner routes, and several learner keys arrive in the query string. This
// deliberately logs only method/status/duration and never path, query, token, or learnerKey.
var safeRequestLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("SupportRoom.Api.SafeRequestLogging");
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();
        safeRequestLogger.LogInformation(
            "HTTP {RequestMethod} responded {StatusCode} in {ElapsedMs:0.0000} ms",
            context.Request.Method,
            context.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds);
    }
});

app.UseExceptionHandler();

// (token, learnerKey) is a composite bearer credential. Responses from any route that accepts
// either half must not be retained by a browser/shared proxy or leaked as a referrer. This is
// intentionally enforced at the pipeline boundary so new actions under these route prefixes
// inherit the protection automatically.
app.Use(async (context, next) =>
{
    if (IsSensitiveLearnerPath(context.Request.Path))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });
    }

    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "SupportRoom API v1"));
}

// Only redirect to HTTPS when a real HTTPS endpoint is bound. In Development the app listens on
// http://localhost:5138 only, so an unconditional UseHttpsRedirection just logs "Failed to
// determine the https port for redirect" on every request.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsSetup.PolicyName);

// Defaults trust forwarded headers only from loopback proxies. Deployments behind another proxy
// must explicitly configure it as trusted rather than accepting a spoofable client header.
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Must run AFTER UseAuthentication so context.User already carries verified claims, and BEFORE
// MapControllers so every action starts with a company resolved and checked. Learner-side
// requests carry no token and pass straight through, resolving their own company from
// TrainingLink.Token instead - see CurrentUserMiddleware.
app.UseMiddleware<CurrentUserMiddleware>();

app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

app.Run();

static bool IsSensitiveLearnerPath(PathString path)
    => path.StartsWithSegments("/api/training-links")
       || path.StartsWithSegments("/api/learning-sessions")
       || path.StartsWithSegments("/api/session-questions")
       || path.StartsWithSegments("/api/chat-messages")
       || path.StartsWithSegments("/api/voice-question")
       || path.StartsWithSegments("/api/lessons/by-link")
       || path.StartsWithSegments("/api/lessons/pdf-pages")
       || path.StartsWithSegments("/hubs/session");

// Exposed for SupportRoom.Api.IntegrationTests' WebApplicationFactory<Program>.
public partial class Program;
