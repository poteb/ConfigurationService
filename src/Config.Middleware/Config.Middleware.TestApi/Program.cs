using pote.Config.Middleware;
using pote.Config.Middleware.TestApi;

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

AddSecretConfiguration<MySecrets>(builder.Services, builder.Configuration, secretResolver);

static T AddSecretConfiguration<T>(IServiceCollection services, IConfiguration configuration, ISecretResolver secretResolver) where T : class, ISecretSettings
{
    T settings = configuration.GetSection(typeof (T).Name).Get<T>()!;
    settings.SecretResolver = secretResolver;
    services.AddSingleton<T>(_ => settings);
    return settings;
}

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