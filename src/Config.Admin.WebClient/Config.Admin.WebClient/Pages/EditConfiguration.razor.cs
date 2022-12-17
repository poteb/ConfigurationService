using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using pote.Config.Admin.WebClient.Components;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class EditConfiguration : IDisposable
{
    private MudExpansionPanels _expansionPanels = null!;
    private bool _allPanelsExpanded;
    private readonly List<ConfigurationContent> _configurationContents = new();
    private Timer? _unsavedChangesTimer;
    private IReadOnlyList<ConfigurationHeader> _headers = new List<ConfigurationHeader>();
    private MudForm _form = null!;
    private bool _formIsValid;

    [Parameter] public string Gid { get; set; } = string.Empty;
    private bool IsNew => string.IsNullOrWhiteSpace(Gid);
    private ConfigurationHeader Header { get; set; } = new();
    private ConfigurationHeader OriginalHeader { get; set; } = null!;
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [Inject] public IApiService ApiService { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    private List<Application> Applications { get; set; } = new();
    private List<ConfigEnvironment> Environments { get; set; } = new();
    private List<Application> UnhandledApplications { get; set; } = new();
    private List<ConfigEnvironment> UnhandledEnvironments { get; set; } = new();
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    private bool HasUnsavedChanges { get; set; }

    private ConfigurationContent ConfigurationContentRef
    {
        set => _configurationContents.Add(value);
    }

    protected override async Task OnInitializedAsync()
    {
        await Load();
        _unsavedChangesTimer = new Timer(_ =>
        {
            var oldHasChanged = HasUnsavedChanges;
            HasUnsavedChanges = !Header.Equals(OriginalHeader);
            if (oldHasChanged != HasUnsavedChanges)
                StateHasChanged();
        }, new AutoResetEvent(false), 1000, 1000);
        await LoadAllHeaders();
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

    private async Task LoadAllHeaders()
    {
        var callResponse = await AdminApiService.GetConfigurations();
        if (callResponse is { IsSuccess: true, Response: { } })
            _headers = ConfigurationMapper.ToClient(callResponse.Response.Configurations.Where(c => c.Id != Header?.Id).ToImmutableList());
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
            Environments = EnvironmentMapper.ToClient(callResponse.Response.Environments);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task<bool> Save()
    {
        await _form.Validate();
        if (!_formIsValid) return false;

        PageError.Reset();
        Header.CreatedUtc = DateTime.UtcNow;
        var reload = Header.Configurations.Any(c => c.Deleted);
        Header.Configurations = Header.Configurations.Where(c => !c.Deleted).ToList();
        var callResponse = await AdminApiService.SaveConfiguration(Header);
        if (callResponse.IsSuccess)
        {
            UpdateUnhandledApplications();
            UpdateUnhandledEnvironments();
            if (IsNew)
                Gid = Header.Id;
            if (reload)
                await Load();
            else
            {
                OriginalHeader = ConfigurationMapper.Copy(Header);
                Console.WriteLine("Update original header");
            }
            return true;
        }

        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        return false;
    }

    // private void Duplicate()
    // {
    //     //var _ = ConfigurationMapper.Copy(Configuration);
    // }

    private async Task Delete()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<DeleteConfigurationDialog>("Delete configuration?", options);
        var result = await dialog.Result;
        if (result.Cancelled) return;
        var softDelete = (bool)result.Data;
        
        Header.Deleted = true;
        PageError.Reset();
        var callResponse = await AdminApiService.DeleteConfiguration(Gid, !softDelete);
        if (!callResponse.IsSuccess)
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
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
            await configurationContent.RunTest();
    }

    public void Dispose() => _unsavedChangesTimer?.Dispose();

    private async Task NameChanged()
    {
        if (_headers.Any(h => h.Name == Header.Name))
        {
            Console.WriteLine("Already exists");
            return;
        }
        Console.WriteLine("All good");
    }

    private string ValidateHeaderName(string s)
    {
        if (string.IsNullOrWhiteSpace(Header.Name)) return "Name is empty";
        if (_headers.All(h => h.Name != Header.Name)) return null!;
        Console.WriteLine("Already exists");
        return "Already exists";
    }

    private Configuration GetOriginalConfiguration(string id)
    {
        return OriginalHeader.Configurations.FirstOrDefault(c => c.Id == id) ?? new Configuration();
    }
}