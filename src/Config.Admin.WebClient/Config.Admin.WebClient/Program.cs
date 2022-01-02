global using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using pote.Config.Admin.WebClient;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<SearchCriteria>();

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration.GetConnectionString("AdminApi")) });

builder.Services.AddMudServices();

builder.Services.AddScoped<IAdminApiService, AdminApiService>();

await builder.Build().RunAsync(); 
