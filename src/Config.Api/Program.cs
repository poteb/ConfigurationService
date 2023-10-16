global using pote.Config.Parser;
global using pote.Config.Shared;
using Df.ServiceControllerExtensions;
using pote.Config.Api.Authentication;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
builder.Services.AddScoped<ApiKeyAuthenticationFilter>();

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
