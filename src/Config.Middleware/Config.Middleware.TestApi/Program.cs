using pote.Config.Middleware;
using pote.Config.Middleware.TestApi;
using pote.Config.Middleware.Web;

var builder = WebApplication.CreateBuilder(args);

var configSettings = new BuilderConfiguration
{
    Application = "Goofy",
    Environment = "Development",
    ApiUri = builder.Configuration.GetValue<string>("ConfigurationApiUri")!,
    WorkingDirectory = ""
};
var environmentSettingsJsonContent = File.ReadAllText($"appsettings.{configSettings.Environment}.json");
_ = builder.AddConfigurationFromApi(configSettings, environmentSettingsJsonContent, () =>
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("X-API-Key", builder.Configuration.GetSection("ApiKey").Value!);
    return client;
}, (_, _) => { }).Result;
builder.Services.AddSecretsResolver(() =>
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("X-API-Key", builder.Configuration.GetSection("ApiKey").Value!);
    return client;
}, configSettings, out var secretResolver);

builder.Services.AddSecretConfiguration<MySecrets>(builder.Configuration, secretResolver);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();