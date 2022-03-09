using Microsoft.AspNetCore.Components;
using MudBlazor;
using pote.Config.Admin.WebClient.Components;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class EditConfiguration
{
    private MudExpansionPanels _expansionPanels = null!;
    private bool _allPanelsExpanded;
    private List<ConfigurationContent> _configurationContents = new();

    [Parameter] public string Gid { get; set; } = string.Empty;
    private bool IsNew => string.IsNullOrWhiteSpace(Gid);
    private ConfigurationHeader Header { get; set; } = new();
    private ConfigurationHeader OriginalHeader { get; set; } = null!;
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [Inject] public IApiService ApiService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    private List<Application> Applications { get; set; } = new();
    private List<ConfigEnvironment> Environments { get; set; } = new();
    private List<Application> UnhandledApplications { get; set; } = new();
    private List<ConfigEnvironment> UnhandledEnvironments { get; set; } = new();
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private ConfigurationContent ConfigurationContentRef
    {
        set => _configurationContents.Add(value);
    }

    protected override async Task OnInitializedAsync()
    {
        await Load();
    }

    private async Task Load()
    {
        if (IsNew)
            Header = new();
        else
        {
            var callResponse = await AdminApiService.GetConfiguration(Gid);
            if (callResponse.IsSuccess && callResponse.Response != null)
            {
                Header = ConfigurationMapper.ToClient(callResponse.Response.Configuration);
                OriginalHeader = ConfigurationMapper.Copy(Header);
                UpdateConfigurationIndex();
            }
            else
            {
                PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
                return;
            }
        }

        await UpdateApplications();
        await UpdateEnvironments();
        UpdateUnhandledApplications();
        UpdateUnhandledEnvironments();
    }

    private void UpdateConfigurationIndex()
    {
        for (var i = 0; i < Header.Configurations.Count; i++)
            Header.Configurations[i].Index = i;
    }

    private void UpdateUnhandledApplications()
    {
        UnhandledApplications.Clear();
        foreach (var application in Applications)
        {
            if (Header.Configurations.Any(c => c.Applications.Any(s => s.Id == application.Id))) continue;
            UnhandledApplications.Add(application);
        }
    }

    private void UpdateUnhandledEnvironments()
    {
        UnhandledEnvironments.Clear();
        foreach (var environment in Environments)
        {
            if (Header.Configurations.Any(c => c.Environments.Any(e => e.Id == environment.Id))) continue;
            UnhandledEnvironments.Add(environment);
        }
    }

    private async Task UpdateApplications()
    {
        var callResponse = await AdminApiService.GetApplications();
        if (callResponse.IsSuccess && callResponse.Response != null)
        {
            Applications = ApplicationMapper.ToClient(callResponse.Response.Applications);
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

    private async Task<bool> Save()
    {
        if (Header.Equals(OriginalHeader)) return true;

        PageError.Reset();
        Header.CreatedUtc = DateTime.UtcNow;
        var reload = Header.Configurations.Any(c => c.Deleted);
        Header.Configurations = Header.Configurations.Where(c => !c.Deleted).ToList();
        var callResponse = await AdminApiService.SaveConfiguration(Header);
        if (callResponse.IsSuccess)
        {
            UpdateUnhandledApplications();
            UpdateUnhandledEnvironments();
            if (reload)
                await Load();
            return true;
        }

        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        return false;
    }

    // private void Duplicate()
    // {
    //     //var _ = ConfigurationMapper.Copy(Configuration);
    // }

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
        if (!_allPanelsExpanded)
            _expansionPanels.ExpandAll();
        else
            _expansionPanels.CollapseAll();

        // var panelsType = typeof(MudExpansionPanels);
        // var panelsField = panelsType.GetField("_panels", BindingFlags.Instance | BindingFlags.NonPublic);
        // var list = panelsField?.GetValue(_expansionPanels) as List<MudExpansionPanel>;
        // if (list == null) return;
        // foreach (var panel in list)
        // {
        //     if (!_allPanelsExpanded)
        //         panel.Expand();
        //     else
        //         panel.Collapse();
        // }

        _allPanelsExpanded = !_allPanelsExpanded;
    }

    private void AddConfiguration()
    {
        Header.Configurations.Add(new Configuration { HeaderId = Header.Id });
        UpdateConfigurationIndex();
    }

    private async Task TestAll()
    {
        _expansionPanels.ExpandAll();
        _allPanelsExpanded = true;
        foreach (var configurationContent in _configurationContents)
        {
            await configurationContent.RunTest();
        }
    }
}