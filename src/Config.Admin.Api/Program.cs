using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using pote.Config.Admin.Api.Services;
using pote.Config.DataProvider.File;
using pote.Config.Parser;
using pote.Config.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(p => p.AddPolicy("allowall", builder =>
{
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

var fileDb = builder.Configuration.GetSection("FileDatabase").GetSection("Directory").Value;
builder.Services.AddScoped<IFileHandler>(_ => new FileHandler(fileDb));
builder.Services.AddScoped<IApplicationDataAccess, ApplicationDataAccess>();
builder.Services.AddScoped<IEnvironmentDataAccess, EnvionmentDataAccess>();
builder.Services.AddScoped<IAdminDataProvider, AdminDataProvider>();
builder.Services.AddScoped<IDependencyGraphService, DependencyGraphService>();

builder.Services.AddScoped<IDataProvider, DataProvider>();
builder.Services.AddScoped<IParser, Parser>();

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