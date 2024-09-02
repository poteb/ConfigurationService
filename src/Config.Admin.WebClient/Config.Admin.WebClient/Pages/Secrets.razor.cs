using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Mappers;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Secrets
{
    public List<SecretHeader> Headers { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
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
        var callResponse = await AdminApiService.GetSecrets();
        if (callResponse.IsSuccess && callResponse.Response != null)
            Headers = SecretMapper.ToClient(callResponse.Response.Secrets);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
        await UpdateApplications();
        await UpdateEnvironments();
    }
    
    private async Task UpdateApplications()
    {
        var callResponse = await AdminApiService.GetApplications();
        if (callResponse.IsSuccess && callResponse.Response != null)
            Applications = ApplicationMapper.ToClient(callResponse.Response.Applications);
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
    
    private void AddNewSecret()
    {
        NavigationManager.NavigateTo("EditSecret");
    }
    
    private void EditSecret(string gid)
    {
        NavigationManager.NavigateTo($"EditSecret/{gid}");
    }
    
    private bool FilterFunc(SecretHeader header)
    {
        if (!string.IsNullOrWhiteSpace(SearchCriteria.SelectedApplication))
        {
            var headerApplications = header.Secrets.SelectMany(c => c.Applications).Distinct();
            if (headerApplications.All(a => a.Name != SearchCriteria.SelectedApplication)) return false;
        }

        if (!string.IsNullOrWhiteSpace(SearchCriteria.SelectedEnvironment))
        {
            var headerEnvironments = header.Secrets.SelectMany(c => c.Environments).Distinct();
            if (headerEnvironments.All(e => e.Name != SearchCriteria.SelectedEnvironment)) return false;
        }

        if (!string.IsNullOrEmpty(SearchCriteria.SearchText) && !header.Name.Contains(SearchCriteria.SearchText, StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    private void ResetSearch()
    {
        SearchCriteria.Reset();
    }
    
    private string GetConfigurationApplicationsAsString(SecretHeader header)
    {
        var applications = header.Secrets.SelectMany(c => c.Applications).Distinct().OrderBy(a => a.Name);
        return string.Join(", ", applications.Select(s => s.Name));
    }

    private string GetConfigurationEnvironmentsAsString(SecretHeader header)
    {
        var environments = header.Secrets.SelectMany(c => c.Environments).Distinct().OrderBy(e => e.Name);
        return string.Join(", ", environments.Select(e => e.Name));
    }
}