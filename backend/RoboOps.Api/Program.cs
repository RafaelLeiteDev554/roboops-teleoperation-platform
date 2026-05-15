using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RoboOps.Api.Models;
using RoboOps.Api.Realtime;
using RoboOps.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("OperatorConsole", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<RoboOpsStore>();
builder.Services.AddSingleton<DataQualityScoring>();
builder.Services.AddSingleton<AuthTokenService>();
builder.Services.AddHostedService<RobotTelemetrySimulator>();
builder.Services.AddSignalR();

var signingKey = builder.Configuration["Auth:SigningKey"] ?? "roboops-local-development-signing-key-change-me";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = "RoboOps",
            ValidAudience = "RoboOps.OperatorConsole",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/telemetry"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReviewData", policy => policy.RequireRole("Admin", "Reviewer", "DataEngineer"));
    options.AddPolicy("CanOperateRobot", policy => policy.RequireRole("Admin", "Operator"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("OperatorConsole");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "RoboOps.Api",
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/auth/login", (LoginRequest request, AuthTokenService tokens) =>
{
    var response = tokens.Login(request);
    return response is null ? Results.Unauthorized() : Results.Ok(response);
});

var api = app.MapGroup("/api").RequireAuthorization();

api.MapGet("/robots", (RoboOpsStore store) => Results.Ok(store.GetRobots()));
api.MapGet("/robots/{id:guid}", (Guid id, RoboOpsStore store) =>
    store.GetRobot(id) is { } robot ? Results.Ok(robot) : Results.NotFound());

api.MapGet("/tasks", (RoboOpsStore store) => Results.Ok(store.GetTasks()));
api.MapGet("/telemetry", (RoboOpsStore store) => Results.Ok(store.GetTelemetry()));
api.MapGet("/sessions", (RoboOpsStore store) => Results.Ok(store.GetSessions()));
api.MapGet("/sessions/{id:guid}", (Guid id, RoboOpsStore store) =>
    store.GetSession(id) is { } session ? Results.Ok(session) : Results.NotFound());

api.MapPost("/sessions", (StartSessionRequest request, RoboOpsStore store) =>
{
    try
    {
        var session = store.StartSession(request);
        return Results.Created($"/api/sessions/{session.Id}", session);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("CanOperateRobot");

api.MapPost("/sessions/{id:guid}/feedback", (Guid id, DataQualityFeedbackRequest request, RoboOpsStore store, DataQualityScoring scoring) =>
{
    try
    {
        return Results.Created($"/api/sessions/{id}/feedback", store.AddFeedback(id, request, scoring));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization("CanReviewData");

api.MapGet("/sessions/{id:guid}/quality", (Guid id, RoboOpsStore store, DataQualityScoring scoring) =>
{
    var session = store.GetSession(id);
    if (session is null)
    {
        return Results.NotFound();
    }

    var telemetry = store.GetTelemetry(session.RobotId);
    return telemetry is null
        ? Results.NotFound()
        : Results.Ok(scoring.Calculate(telemetry, store.GetFeedback(id)));
});

api.MapGet("/pipelines", (RoboOpsStore store) => Results.Ok(store.GetPipelineRuns()))
    .RequireAuthorization("CanReviewData");

api.MapPost("/webrtc/offer", (WebRtcOfferRequest request) =>
{
    var response = new WebRtcOfferResponse(
        "mock-answer-sdp-for-local-portfolio-demo",
        "answer",
        ["stun:stun.l.google.com:19302"]);

    return Results.Ok(response);
}).RequireAuthorization("CanOperateRobot");

app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();

public partial class Program;
