using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class EditConfiguration
{
    [Parameter] public string Gid { get; set; } = string.Empty;
    public bool IsNew => string.IsNullOrWhiteSpace(Gid);
    public Configuration Configuration { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    public List<ConfigSystem> Systems { get; set; } = new();
    public List<ConfigEnvironment> Environments { get; set; } = new();
    public ConfigSystem SelectedSystem { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (IsNew)
            Configuration = new Configuration();
        else
        {
            var callResponse = await AdminApiService.GetConfiguration(Gid);
            if (callResponse.IsSuccess && callResponse.Response != null)
                Configuration = ConfigurationMapper.ToClient(callResponse.Response.Configuration);
            else
            {
                PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
                return;
            }
        }
        await UpdateSystems();
        await UpdateEnvironments();
    }

    private async Task UpdateSystems()
    {
        var callResponse = await AdminApiService.GetSystems();
        if (callResponse.IsSuccess && callResponse.Response != null)
        {
            Systems = SystemMapper.ToClient(callResponse.Response.Systems);
            Systems.ForEach(s => s.IsSelected = Configuration.Systems.Any(x => x.Id == s.Id));
        }
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task UpdateEnvironments()
    {
        var callResponse = await AdminApiService.GetEnvironments();
        if (callResponse.IsSuccess && callResponse.Response != null)
        {
            Environments = EnvironmentMapper.ToClient(callResponse.Response.Environments);
            Environments.ForEach(e => e.IsSelected = Configuration.Environments.Any(x => x.Id == e.Id));
        }
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    public object GetSortValue(ConfigSystem system)
    {
        return $"{(system.IsSelected ? 1 : 2)} {system.Name}";
    }

    public object GetSortValue(ConfigEnvironment system)
    {
        return $"{(system.IsSelected ? 1 : 2)} {system.Name}";
    }

    private async Task<bool> Save()
    {
        PageError.Reset();
        Configuration.CreatedUtc = DateTime.UtcNow;
        Configuration.Systems.Clear();
        Configuration.Systems.AddRange(Systems.Where(s => s.IsSelected));
        Configuration.Environments.Clear();
        Configuration.Environments.AddRange(Environments.Where(e => e.IsSelected));
        var callResponse = await AdminApiService.SaveConfiguration(Configuration);
        if (callResponse.IsSuccess)
            return true;
        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        return false;
    }

    private void Duplicate()
    {
        Configuration = ConfigurationMapper.Copy(Configuration);
    }

    private async void Delete()
    {
        Configuration.Deleted = true;
        PageError.Reset();
        if (await Save())
            NavigationManager.NavigateTo("/");
    }
}