global using pote.Config.Parser;
global using pote.Config.Shared;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IParser, Parser>();
var fileDb = builder.Configuration.GetSection("FileDatabase").GetSection("Directory").Value;
builder.Services.AddScoped<IFileHandler>(_ => new FileHandler(fileDb));
builder.Services.AddScoped<IDataProvider, DataProvider>();
builder.Services.AddScoped<IApplicationDataAccess, ApplicationDataAccess>();
builder.Services.AddScoped<IEnvironmentDataAccess, EnvironmentDataAccess>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
app.UseCors("AllowOrigin");
//app.UseAuthorization();

app.MapControllers();

app.Run();
