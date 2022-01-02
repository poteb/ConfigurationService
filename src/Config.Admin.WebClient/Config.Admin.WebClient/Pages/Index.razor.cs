using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Index
{
    public List<Configuration> Configurations { get; set; } = new();
    public List<ConfigSystem> Systems { get; set; } = new();
    public List<ConfigEnvironment> Environments { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    [Inject] public SearchCriteria SearchCriteria { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await Load();
    }

    private async Task Load()
    {
        PageError.Reset();
        var callResponse = await AdminApiService.GetConfigurations();
        if (callResponse.IsSuccess && callResponse.Response != null)
            Configurations = ConfigurationMapper.ToClient(callResponse.Response.Configurations);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        await UpdateSystems();
        await UpdateEnvironments();
    }

    private async Task UpdateSystems()
    {
        var callResponse = await AdminApiService.GetSystems();
        if (callResponse.IsSuccess && callResponse.Response != null)
            Systems = SystemMapper.ToClient(callResponse.Response.Systems);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task UpdateEnvironments()
    {
        var callResponse = await AdminApiService.GetEnvironments();
        if (callResponse.IsSuccess && callResponse.Response != null)
            Environments = EnvironmentMapper.ToClient(callResponse.Response.Environments);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private void AddNewConfiguration()
    {
        NavigationManager.NavigateTo("EditConfiguration");
    }

    private void EditConfiguration(string gid)
    {
        NavigationManager.NavigateTo($"EditConfiguration/{gid}");
    }

    private bool FilterFunc(Configuration configuration)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (!string.IsNullOrWhiteSpace(SearchCriteria.SelectedSystem))
        {
            if (configuration.Systems.All(s => s.Name != SearchCriteria.SelectedSystem))
                return false;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (!string.IsNullOrWhiteSpace(SearchCriteria.SelectedEnvironment))
        {
            if (configuration.Environments.All(e => e.Name != SearchCriteria.SelectedEnvironment))
                return false;
        }

        if (!string.IsNullOrEmpty(SearchCriteria.SearchText) && !configuration.Name.Contains(SearchCriteria.SearchText, StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    private void ResetSearch()
    {
        SearchCriteria.Reset();
    }
}