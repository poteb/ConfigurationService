using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;
using pote.Config.Admin.WebClient.Components;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Misc;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class EditConfiguration : IDisposable, IConfigurationActions
{
    private MudExpansionPanels _expansionPanels = null!;
    private bool _allPanelsExpanded;
    private readonly List<ConfigurationContent> _configurationContents = new();
    private Timer? _unsavedChangesTimer;
    private IReadOnlyList<ConfigurationHeader> _headers = new List<ConfigurationHeader>();
    private MudForm _form = null!;
    private bool _formIsValid;
    private bool _loadingHistory;
    private bool _disableReorderButtons => Header.Configurations.Count <= 1;
    private Settings _settings = new();

    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Parameter] public string Gid { get; set; } = string.Empty;
    private bool IsNew => string.IsNullOrWhiteSpace(Gid);
    private ConfigurationHeader Header { get; set; } = new();
    private ConfigurationHeader OriginalHeader { get; set; } = null!;
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [Inject] public IApiService ApiService { get; set; } = null!;
    [Inject] public ISettingsService SettingsService { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    private List<Application> Applications { get; set; } = new();
    private List<ConfigEnvironment> Environments { get; set; } = new();
    private List<Application> UnhandledApplications { get; set; } = new();
    private List<ConfigEnvironment> UnhandledEnvironments { get; set; } = new();
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    private bool HasUnsavedChanges { get; set; }
    [Inject] public ISnackbar SnackbarService { get; set; } = null!;

    private ConfigurationContent ConfigurationContentRef
    {
        set => _configurationContents.Add(value);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Gid == Header.Id) return;
        await Load();
        _unsavedChangesTimer = new Timer(_ => { UpdateHasUnsavedChanges(); }, new AutoResetEvent(false), 1000, 1000);
        await LoadAllHeaders();
    }

    private void UpdateHasUnsavedChanges()
    {
        var oldHasChanged = HasUnsavedChanges;
        HasUnsavedChanges = !Header.Equals(OriginalHeader);
        if (oldHasChanged != HasUnsavedChanges)
            StateHasChanged();
    }

    private async Task Load()
    {
        await LoadSettings();
        
        if (IsNew)
        {
            Header = new();
            Header.Configurations.Add(new Configuration());
        }
        else
        {
            var callResponse = await AdminApiService.GetConfiguration(Gid);
            if (callResponse is {IsSuccess: true, Response: { }})
            {
                Header = ConfigurationMapper.ToClient(callResponse.Response.Configuration);
                if (Header.IsJsonEncrypted || _settings.EncryptAllJson)
                    IsJsonEncryptedCheck(Header.IsJsonEncrypted || _settings.EncryptAllJson);
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
        
        if (Header.IsJsonEncrypted || _settings.EncryptAllJson)
            IsJsonEncryptedCheck(Header.IsJsonEncrypted || _settings.EncryptAllJson);
    }

    private async Task LoadAllHeaders()
    {
        var callResponse = await AdminApiService.GetConfigurations();
        if (callResponse is {IsSuccess: true, Response: { }})
            _headers = ConfigurationMapper.ToClient(callResponse.Response.Configurations.Where(c => c.Id != Header?.Id).ToImmutableList());
    }

    private async Task LoadSettings()
    {
        var callResponse = await SettingsService.GetSettings();
        if (callResponse is {IsSuccess: true, Response: not null})
        {
            _settings = SettingsMapper.ToClient(callResponse.Response.Settings);
        }
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
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
        if (callResponse is {IsSuccess: true, Response: not null})
        {
            Applications = ApplicationMapper.ToClient(callResponse.Response.Applications);
        }
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task UpdateEnvironments()
    {
        var callResponse = await AdminApiService.GetEnvironments();
        if (callResponse is {IsSuccess: true, Response: not null})
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
            }

            return true;
        }

        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        return false;
    }

    private async Task Duplicate()
    {
        var dialogParameters = new DialogParameters
        {
            {nameof(DuplicateHeaderDialog.NewHeaderName), $"{Header.Name} COPY"}
        };
        var dialog = await DialogService.ShowAsync<DuplicateHeaderDialog>("Duplicate Header", dialogParameters);
        var result = await dialog.Result;
        if (result.Canceled) return;
        var copy = ConfigurationMapper.Copy(Header, true);
        copy.Name = (string) result.Data;
        var callResponse = await AdminApiService.SaveConfiguration(copy);
        if (callResponse.IsSuccess)
        {
            SnackbarService.Add("Header duplicated. Click to open it.", Severity.Success, config =>
            {
                config.Onclick = _ =>
                {
                    NavigationManager.NavigateTo($"EditConfiguration/{copy.Id}", new NavigationOptions { ForceLoad = false });
                    return Task.CompletedTask;
                };
            });
            return;
        }

        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    public void DuplicateConfiguration(Configuration configuration)
    {
        var copy = ConfigurationMapper.Copy(configuration, true);
        foreach (var c in Header.Configurations.Where(c => c.Index > configuration.Index)) 
            c.Index++;
        copy.Index = configuration.Index + 1;
        var realIndex = Header.Configurations.IndexOf(configuration) + 1;
        Header.Configurations.Insert(realIndex, copy);
        StateHasChanged();
    }

    private async Task Delete()
    {
        var options = new DialogOptions {CloseOnEscapeKey = true};
        var dialog = await DialogService.ShowAsync<DeleteConfigurationDialog>("Delete configuration?", options);
        var result = await dialog.Result;
        if (result.Canceled) return;
        var softDelete = (bool) result.Data;

        //Header.Deleted = true;
        PageError.Reset();
        var callResponse = await AdminApiService.DeleteConfiguration(Gid, !softDelete);
        if (!callResponse.IsSuccess)
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        NavigationManager.NavigateTo("/");
    }

    private string GetExpandAllConfigurationsButtonIcon()
    {
        return !_allPanelsExpanded ? Icons.Material.Filled.KeyboardDoubleArrowDown : Icons.Material.Filled.KeyboardDoubleArrowUp;
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

        _allPanelsExpanded = !_allPanelsExpanded;
    }

    private async Task AddConfiguration()
    {
        Header.Configurations.Add(new Configuration {HeaderId = Header.Id, IsNew = true});
        UpdateConfigurationIndex();
        await JsRuntime.InvokeVoidAsync("scrollIntoView", "bottom");
    }

    private async Task TestAll()
    {
        _expansionPanels.ExpandAll();
        _allPanelsExpanded = true;
        foreach (var configurationContent in _configurationContents)
            await configurationContent.RunTest();
    }

    private async Task ShowReorderConfigurations()
    {
        var list = new List<Configuration>(Header.Configurations);
        var dialogParameters = new DialogParameters
        {
            {nameof(ReorderConfigurationsDialog.Configurations), list}
        };
        var dialog = await DialogService.ShowAsync<ReorderConfigurationsDialog>("Reorder configurations", dialogParameters);
        var result = await dialog.Result;
        if (result.Canceled) return;
        foreach (var c in list.OrderBy(x => x.Index))
        {
            var found = Header.Configurations.First(d => d.Id == c.Id);
            found.Index = c.Index;
            Header.Configurations.Sort();
        }
    }

    public void Dispose() => _unsavedChangesTimer?.Dispose();

    private string ValidateHeaderName(string s)
    {
        if (string.IsNullOrWhiteSpace(Header.Name)) return "Name is empty";
        if (_headers.All(h => h.Name != Header.Name)) return null!;
        return "Already exists";
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        UpdateHasUnsavedChanges();
        if (!HasUnsavedChanges) return;
        var isConfirmed = await JsRuntime.InvokeAsync<bool>("confirm", "You have unsaved changes. Click OK to lose the changes and continue. Click Cancel to stay on the page.");

        if (!isConfirmed)
        {
            context.PreventNavigation();
        }
    }

    private async Task LoadHistory()
    {
        _loadingHistory = true;
        foreach (var configuration in Header.Configurations)
            configuration.History.Clear();

        var callResponse = await AdminApiService.GetHeaderHistory(Header.Id, 1, 10);
        if (callResponse is {IsSuccess: true, Response: { }})
        {
            var response = callResponse.Response;
            foreach (var historyHeader in response.History)
            {
                foreach (var historyConfiguration in historyHeader.Configurations)
                {
                    var configuration = Header.Configurations.FirstOrDefault(c => c.Id == historyConfiguration.Id);
                    if (configuration == null) continue;
                    var uiHistoryConfiguration = ConfigurationMapper.ToClient(historyConfiguration);
                    configuration.History.Add(uiHistoryConfiguration);
                }
            }
        }
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());

        _loadingHistory = false;
    }
    
    private string IsJsonEncryptedCheckboxText()
    {
        return Header.IsJsonEncrypted ? "Encryption (ON)" : "Encryption (OFF)";
    }

    private void IsJsonEncryptedCheck(bool b)
    {
        Header.IsJsonEncrypted = b;
        Header.IsJsonEncryptedForced = _settings.EncryptAllJson;
        foreach (var c in Header.Configurations)
        {
            c.IsJsonEncrypted = Header.IsJsonEncrypted;
            c.IsJsonEncryptedForced = Header.IsJsonEncrypted || _settings.EncryptAllJson;
        }
    }
}