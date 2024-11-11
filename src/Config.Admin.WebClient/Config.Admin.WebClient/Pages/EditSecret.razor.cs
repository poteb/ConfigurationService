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

public partial class EditSecret : IDisposable, ISecretActions
{
    private MudExpansionPanels _expansionPanels = null!;
    private bool _allPanelsExpanded;
    private readonly List<SecretContent> _secretContents = new();
    private Timer? _unsavedChangesTimer;
    private IReadOnlyList<SecretHeader> _headers = new List<SecretHeader>();
    private MudForm _form = null!;
    private bool _formIsValid;
    private bool _loadingHistory;
    private bool _disableReorderButtons => Header.Secrets.Count <= 1;
    private Settings _settings = new();
    
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Parameter] public string Gid { get; set; } = string.Empty;
    private bool IsNew => string.IsNullOrWhiteSpace(Gid);
    private SecretHeader Header { get; set; } = new();
    private SecretHeader OriginalHeader { get; set; } = null!;
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
    
    private SecretContent SecretContentRef
    {
        set => _secretContents.Add(value);
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
            Header.Secrets.Add(new Secret());
        }
        else
        {
            var callResponse = await AdminApiService.GetSecret(Gid);
            if (callResponse is {IsSuccess: true, Response: { }})
            {
                Header = SecretMapper.ToClient(callResponse.Response.Secret);
                OriginalHeader = SecretMapper.Copy(Header);
                UpdateSecretIndex();
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
        var callResponse = await AdminApiService.GetSecrets();
        if (callResponse is {IsSuccess: true, Response: { }})
            _headers = SecretMapper.ToClient(callResponse.Response.Secrets.Where(c => c.Id != Header?.Id).ToImmutableList());
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

    private void UpdateSecretIndex()
    {
        for (var i = 0; i < Header.Secrets.Count; i++)
            Header.Secrets[i].Index = i;
    }

    private void UpdateUnhandledApplications()
    {
        UnhandledApplications.Clear();
        foreach (var application in Applications)
        {
            if (Header.Secrets.Any(c => c.Applications.Any(s => s.Id == application.Id))) continue;
            UnhandledApplications.Add(application);
        }
    }

    private void UpdateUnhandledEnvironments()
    {
        UnhandledEnvironments.Clear();
        foreach (var environment in Environments)
        {
            if (Header.Secrets.Any(c => c.Environments.Any(e => e.Id == environment.Id))) continue;
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
        var reload = Header.Secrets.Any(c => c.Deleted);
        Header.Secrets = Header.Secrets.Where(c => !c.Deleted).ToList();
        var callResponse = await AdminApiService.SaveSecret(Header);
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
                OriginalHeader = SecretMapper.Copy(Header);
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
        var copy = SecretMapper.Copy(Header, true);
        copy.Name = (string) result.Data;
        var callResponse = await AdminApiService.SaveSecret(copy);
        if (callResponse.IsSuccess)
        {
            SnackbarService.Add("Header duplicated. Click to open it.", Severity.Success, config =>
            {
                config.Onclick = _ =>
                {
                    NavigationManager.NavigateTo($"EditSecret/{copy.Id}", new NavigationOptions { ForceLoad = false });
                    return Task.CompletedTask;
                };
            });
            return;
        }

        PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    public void DuplicateSecret(Secret secret)
    {
        var copy = SecretMapper.Copy(secret, true);
        foreach (var c in Header.Secrets.Where(c => c.Index > secret.Index)) 
            c.Index++;
        copy.Index = secret.Index + 1;
        var realIndex = Header.Secrets.IndexOf(secret) + 1;
        Header.Secrets.Insert(realIndex, copy);
        StateHasChanged();
    }

    private async Task Delete()
    {
        var options = new DialogOptions {CloseOnEscapeKey = true};
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Delete secret?", options);
        var result = await dialog.Result;
        if (result.Canceled) return;

        //Header.Deleted = true;
        PageError.Reset();
        var callResponse = await AdminApiService.DeleteSecret(Gid);
        if (!callResponse.IsSuccess)
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        NavigationManager.NavigateTo("/");
    }

    private string GetExpandAllSecretsButtonIcon()
    {
        return !_allPanelsExpanded ? Icons.Material.Filled.KeyboardDoubleArrowDown : Icons.Material.Filled.KeyboardDoubleArrowUp;
    }

    private string GetExpandAllSecretsButtonText()
    {
        return !_allPanelsExpanded ? "Expand all" : "Collapse all";
    }

    private void ExpandAllSecrets()
    {
        if (!_allPanelsExpanded)
            _expansionPanels.ExpandAll();
        else
            _expansionPanels.CollapseAll();

        _allPanelsExpanded = !_allPanelsExpanded;
    }

    private async Task AddSecret()
    {
        Header.Secrets.Add(new Secret {HeaderId = Header.Id, IsNew = true});
        UpdateSecretIndex();
        await JsRuntime.InvokeVoidAsync("scrollIntoView", "bottom");
    }

    private async Task ShowReorderSecrets()
    {
        var list = new List<Secret>(Header.Secrets);
        var dialogParameters = new DialogParameters
        {
            {nameof(ReorderSecretsDialog.Secrets), list}
        };
        var dialog = await DialogService.ShowAsync<ReorderSecretsDialog>("Reorder secrets", dialogParameters);
        var result = await dialog.Result;
        if (result.Canceled) return;
        foreach (var c in list.OrderBy(x => x.Index))
        {
            var found = Header.Secrets.First(d => d.Id == c.Id);
            found.Index = c.Index;
            Header.Secrets.Sort();
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

    // private async Task LoadHistory()
    // {
    //     _loadingHistory = true;
    //     foreach (var secret in Header.Secrets)
    //         secret.History.Clear();
    //
    //     var callResponse = await AdminApiService.GetSecretHeaderHistory(Header.Id, 1, 10);
    //     if (callResponse is {IsSuccess: true, Response: { }})
    //     {
    //         var response = callResponse.Response;
    //         foreach (var historyHeader in response.History)
    //         {
    //             foreach (var historySecret in historyHeader.Secrets)
    //             {
    //                 var secret = Header.Secrets.FirstOrDefault(c => c.Id == historySecret.Id);
    //                 if (secret == null) continue;
    //                 var uiHistorySecret = SecretMapper.ToClient(historySecret);
    //                 secret.History.Add(uiHistorySecret);
    //             }
    //         }
    //     }
    //     else
    //         PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    //
    //     _loadingHistory = false;
    // }
}