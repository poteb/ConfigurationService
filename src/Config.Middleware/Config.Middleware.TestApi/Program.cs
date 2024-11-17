using pote.Config.Middleware;
using pote.Config.Middleware.TestApi;
using pote.Config.Middleware.Web;

var builder = WebApplication.CreateBuilder(args);

var configSettings = builder.Services.AddBuilderConfiguration("Goofy", "Development", builder.Configuration.GetValue<string>("ConfigurationApiUri")!, "");
var environmentSettingsJsonContent = File.ReadAllText($"appsettings.{configSettings.Environment}.json");
await builder.AddConfigurationFromApi(configSettings, environmentSettingsJsonContent, builder.Configuration.GetSection("ApiKey").Value!, (_, _) => { });
var secretsResolver = builder.Services.AddSecretsResolver(configSettings, builder.Configuration.GetSection("ApiKey").Value!);
//builder.Services.AddSecretConfiguration<MySecrets>(builder.Configuration, secretsResolver);

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