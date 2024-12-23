global using pote.Config.Parser;
global using pote.Config.Shared;
using Df.ServiceControllerExtensions;
using Microsoft.OpenApi.Models;
using pote.Config.Auth;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

builder.Host.UseSerilog((context ,services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services).WriteTo.Console()
);

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IParser, Parser>();
var fileDb = builder.Configuration.GetSection("FileDatabase").GetSection("Directory").Value;
builder.Services.AddScoped<IFileHandler>(_ => new FileHandler(fileDb));
builder.Services.AddScoped<IDataProvider, DataProvider>();
builder.Services.AddScoped<IApplicationDataAccess, ApplicationDataAccess>();
builder.Services.AddScoped<IEnvironmentDataAccess, EnvironmentDataAccess>();
builder.Services.AddScoped<ISecretDataAccess, SecretDataAccess>();

builder.Services.AddScoped<IApiKeyValidation, ApiKeyValidation>();
builder.Services.AddScoped<ApiKeyAuthenticationFilter>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
app.UseCors("allowall");
//app.UseAuthorization();

app.MapControllers();

app.Run();
