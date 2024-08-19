using pote.Config.Middleware;

var builder = WebApplication.CreateBuilder(args);

var configSettings = new BuilderConfiguration
{
    Application = "Goofy",
    Environment = "Development",
    ApiUri = builder.Configuration.GetValue<string>("ConfigurationApiUri"),
    WorkingDirectory = ""
};
var environmentSettingsJsonContent = File.ReadAllText($"appsettings.{configSettings.Environment}.json");
_ = builder.AddConfigurationFromApi(configSettings, environmentSettingsJsonContent, () => new HttpClient(), (s, ex) => { }).Result;


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();