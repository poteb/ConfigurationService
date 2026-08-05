using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Df.ServiceControllerExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using pote.Config.Admin.Api.Auth;
using pote.Config.Admin.Api.Services;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;
using pote.Config.DataProvider.SqlServer;
using pote.Config.Parser;
using pote.Config.Shared;
using FileDataProvider = pote.Config.DataProvider.File.DataProvider;
using SqlServerDataProvider = pote.Config.DataProvider.SqlServer.DataProvider;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

builder.Host.UseSerilog((context ,services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services).WriteTo.Console()
);

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Session token from POST api/auth/login. Authorization: Bearer <token>",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(p => p.AddPolicy("allowall", policy =>
{
    var origins = builder.Configuration.GetSection("WithOrigins").Get<string[]>();
    if (origins == null)
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        return;
    }
    policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
}));

builder.Services.AddConfiguration<EncryptionSettings>(builder.Configuration);

var dataProviderType = builder.Configuration["DataProvider"] ?? "SqlServer";
if (dataProviderType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    var connStr = builder.Configuration["SqlServer:ConnectionString"];
    builder.Services.AddSingleton(new SqlConnectionFactory(connStr!));
    builder.Services.AddScoped<IApplicationDataAccess, pote.Config.DataProvider.SqlServer.ApplicationDataAccess>();
    builder.Services.AddScoped<IEnvironmentDataAccess, pote.Config.DataProvider.SqlServer.EnvironmentDataAccess>();
    builder.Services.AddScoped<ISecretDataAccess, pote.Config.DataProvider.SqlServer.SecretDataAccess>();
    builder.Services.AddScoped<IAdminDataProvider, pote.Config.DataProvider.SqlServer.AdminDataProvider>();
    builder.Services.AddScoped<IAuditLogHandler, pote.Config.DataProvider.SqlServer.AuditLogHandler>();
    builder.Services.AddScoped<IDataProvider, SqlServerDataProvider>();
    builder.Services.AddScoped<IUserDataAccess, pote.Config.DataProvider.SqlServer.UserDataAccess>();
}
else
{
    var fileDb = builder.Configuration.GetSection("FileDatabase").GetSection("Directory").Value;
    builder.Services.AddScoped<IFileHandler>(_ => new FileHandler(fileDb));
    builder.Services.AddScoped<IApplicationDataAccess, pote.Config.DataProvider.File.ApplicationDataAccess>();
    builder.Services.AddScoped<IEnvironmentDataAccess, pote.Config.DataProvider.File.EnvironmentDataAccess>();
    builder.Services.AddScoped<ISecretDataAccess, pote.Config.DataProvider.File.SecretDataAccess>();
    builder.Services.AddScoped<IAdminDataProvider, pote.Config.DataProvider.File.AdminDataProvider>();
    builder.Services.AddScoped<IAuditLogHandler, pote.Config.DataProvider.File.AuditLogHandler>();
    builder.Services.AddScoped<IDataProvider, FileDataProvider>();
    builder.Services.AddScoped<IUserDataAccess, pote.Config.DataProvider.File.UserDataAccess>();
}
builder.Services.AddScoped<IDependencyGraphService, DependencyGraphService>();
builder.Services.AddScoped<IParser, Parser>();

// Auth provider selection (ADR-0002): the app consumes claims; the provider
// decides how requests become claims. Only "Local" is implemented.
var authSettings = builder.Configuration.GetSection("Auth").Get<AuthSettings>() ?? new AuthSettings();
builder.Services.AddSingleton(authSettings);
IAuthProviderSetup authProvider = authSettings.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase)
    ? new LocalAuthProviderSetup()
    : throw new InvalidOperationException($"Unknown Auth:Provider '{authSettings.Provider}'. Only 'Local' is supported.");
builder.Services.AddSingleton(authProvider);
authProvider.ConfigureServices(builder.Services, builder.Configuration);
authProvider.ConfigureAuthentication(builder.Services.AddAuthentication(AuthPolicies.SchemeName));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.RealUser, p => p.RequireAuthenticatedUser()
        .RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == AuthPolicies.GuestClaim)));
    options.AddPolicy(AuthPolicies.AdminOnly, p => p.RequireAuthenticatedUser()
        .RequireRole(pote.Config.DbModel.UserRoles.Admin)
        .RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == AuthPolicies.GuestClaim)));
    options.AddPolicy(AuthPolicies.GuestOnly, p => p.RequireAuthenticatedUser()
        .RequireClaim(AuthPolicies.GuestClaim));
    // Endpoints without an explicit attribute require a real (non-guest) user.
    options.FallbackPolicy = options.GetPolicy(AuthPolicies.RealUser);
});

// Brute-force and resource-exhaustion mitigation on the anonymous auth endpoints.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});

var app = builder.Build();

// Fail fast when the Local provider has no user storage (ADR-0005), then seed
// the guest bootstrap user if the store is empty.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var authService = scope.ServiceProvider.GetRequiredService<pote.Config.Admin.Api.Auth.AuthService>();
        await authService.EnsureGuestSeeded(CancellationToken.None);
    }
    catch (NotSupportedException ex)
    {
        Log.Fatal(ex, "Startup aborted: {Message}", ex.Message);
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("allowall");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();