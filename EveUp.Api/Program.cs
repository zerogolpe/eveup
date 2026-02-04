using EveUp.Api.Configuration;
using EveUp.Api.Middleware;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using EveUp.Infrastructure.Jobs;
using EveUp.Infrastructure.Repositories;
using EveUp.Infrastructure.Services;
using EveUp.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// HTTP Context Accessor for audit logging
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, EveUp.Api.Services.CurrentUserService>();

// DbContext
builder.Services.AddDbContext<EveUpDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ITokenService>(sp => sp.GetRequiredService<TokenService>());
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IDenunciationService, DenunciationService>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IDenunciationRepository, DenunciationRepository>();
builder.Services.AddScoped<IContestationRepository, ContestationRepository>();

// Infrastructure services (mock para MVP)
builder.Services.AddScoped<IPspProvider, MockPspProvider>();
builder.Services.AddScoped<INotificationService, MockNotificationService>();

// File Storage
builder.Services.AddSingleton<IFileStorage>(sp =>
{
    var storagePath = builder.Configuration.GetValue<string>("FileStorage:LocalPath")
        ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    var baseUrl = builder.Configuration.GetValue<string>("FileStorage:BaseUrl")
        ?? "http://localhost:5297";
    return new EveUp.Infrastructure.Storage.LocalFileStorage(storagePath, baseUrl);
});

// Background jobs
builder.Services.AddScoped<TimeoutCheckerJob>();
builder.Services.AddScoped<TokenCleanupJob>();
builder.Services.AddScoped<UnbanExpiredUsersJob>();
builder.Services.AddScoped<AutoConfirmAttendanceJob>();

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

// Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(context.ModelState));
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

// Middleware order matters!
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("EveUpPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();

// Register recurring Hangfire jobs
RecurringJob.AddOrUpdate<TimeoutCheckerJob>(
    "timeout-checker",
    job => job.ExecuteAsync(),
    "*/15 * * * *"); // Every 15 minutes

RecurringJob.AddOrUpdate<TokenCleanupJob>(
    "token-cleanup",
    job => job.ExecuteAsync(),
    Cron.Daily); // Once per day at midnight

RecurringJob.AddOrUpdate<UnbanExpiredUsersJob>(
    "unban-expired-users",
    job => job.ExecuteAsync(),
    Cron.Hourly); // Every hour to check for expired bans

RecurringJob.AddOrUpdate<AutoConfirmAttendanceJob>(
    "auto-confirm-attendance",
    job => job.ExecuteAsync(),
    Cron.Hourly); // Every hour to auto-confirm attendances after 24h

app.Run();
