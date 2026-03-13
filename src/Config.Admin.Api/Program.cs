using System.Text.Json.Serialization;
using Df.ServiceControllerExtensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using pote.Config.Admin.Api.Services;
using pote.Config.Auth;
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
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: My_API_Key",
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Scheme = "ApiKeyScheme",
                Name = "X-API-Key",
                In = ParameterLocation.Header
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

var dataProviderType = builder.Configuration["DataProvider"] ?? "File";
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
}
builder.Services.AddScoped<IDependencyGraphService, DependencyGraphService>();
builder.Services.AddScoped<IParser, Parser>();

builder.Services.AddScoped<IApiKeyValidation, ApiKeyValidation>();
builder.Services.AddScoped<ApiKeyAuthenticationFilter>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("allowall");

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapControllers();

app.Run();