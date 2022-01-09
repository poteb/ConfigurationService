using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class EditConfiguration
{
    private MudExpansionPanels _expansionPanels = null!;
    private bool _allPanelsExpanded;

    [Parameter] public string Gid { get; set; } = string.Empty;
    public bool IsNew => string.IsNullOrWhiteSpace(Gid);
    public ConfigurationHeader Header { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [Inject] public IApiService ApiService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    public List<ConfigSystem> Systems { get; set; } = new();
    public List<ConfigEnvironment> Environments { get; set; } = new();
    public ConfigSystem SelectedSystem { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;



    protected override async Task OnInitializedAsync()
    {
        if (IsNew)
            Header = new();
        else
        {
            var callResponse = await AdminApiService.GetConfiguration(Gid);
            if (callResponse.IsSuccess && callResponse.Response != null)
                Header = ConfigurationMapper.ToClient(callResponse.Response.Configuration);
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
            //Systems.ForEach(s => s.IsSelected = Configuration.Systems.Any(x => x.Id == s.Id));
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
            //Environments.ForEach(e => e.IsSelected = Configuration.Environments.Any(x => x.Id == e.Id));
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
        Header.CreatedUtc = DateTime.UtcNow;
        //Configuration.Systems.Clear();
        //Configuration.Systems.AddRange(Systems.Where(s => s.IsSelected));
        //Configuration.Environments.Clear();
        //Configuration.Environments.AddRange(Environments.Where(e => e.IsSelected));
        var callResponse = await AdminApiService.SaveConfiguration(Header);
        if (callResponse.IsSuccess)
            return true;
        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        return false;
    }

    private void Duplicate()
    {
        //var _ = ConfigurationMapper.Copy(Configuration);
    }

    private async void Delete()
    {
        Header.Deleted = true;
        PageError.Reset();
        if (await Save())
            NavigationManager.NavigateTo("/");
    }

    private string GetExpandAllConfigurationsButtonIcon()
    {
        return !_allPanelsExpanded ? Icons.Filled.KeyboardDoubleArrowDown : Icons.Filled.KeyboardDoubleArrowUp;
    }

    private string GetExpandAllConfigurationsButtonText()
    {
        return !_allPanelsExpanded ? "Expand all" : "Collapse all";
    }

    private void ExpandAllConfigurations()
    {
        var panelsType = typeof(MudExpansionPanels);
        var panelsField = panelsType.GetField("_panels", BindingFlags.Instance | BindingFlags.NonPublic);
        var list = panelsField?.GetValue(_expansionPanels) as List<MudExpansionPanel>;
        if (list == null) return;
        foreach (var panel in list)
        {
            if (!_allPanelsExpanded)
                panel.Expand();
            else
                panel.Collapse();
        }

        _allPanelsExpanded = !_allPanelsExpanded;
    }
}