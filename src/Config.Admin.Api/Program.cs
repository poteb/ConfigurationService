using pote.Config.Admin.Api.Services;
using pote.Config.DataProvider.File;
using pote.Config.Parser;
using pote.Config.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var fileDb = builder.Configuration.GetSection("FileDatabase").GetSection("Directory").Value;
builder.Services.AddScoped<IFileHandler>(_ => new FileHandler(fileDb));
builder.Services.AddScoped<IApplicationDataAccess, ApplicationDataAccess>();
builder.Services.AddScoped<IEnvironmentDataAccess, EnvionmentDataAccess>();
builder.Services.AddScoped<IAdminDataProvider, AdminDataProvider>();
builder.Services.AddScoped<IDependencyGraphService, DependencyGraphService>();

builder.Services.AddScoped<IDataProvider, DataProvider>();
builder.Services.AddScoped<IParser, Parser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowOrigin");
app.UseAuthorization();

app.MapControllers();

app.Run();
