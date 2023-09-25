global using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using pote.Config.Admin.WebClient;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<SearchCriteria>();

builder.Services.AddHttpClient("AdminApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("AdminApi")!);
});
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("Api")!);
});

builder.Services.AddMudServices();

builder.Services.AddScoped<IAdminApiService, AdminApiService>();
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IDependencyGraphApiService, DependencyGraphApiService>();
builder.Services.AddScoped<IConfigurationTestService, ConfigurationTestService>();
builder.Services.AddScoped<IAllConfigurationsTestService, AllConfigurationsTestService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();


await builder.Build().RunAsync(); 
