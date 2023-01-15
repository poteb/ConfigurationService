using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Environments
{
    private bool _usageLoaded;
    
    public List<ConfigEnvironment> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    [Inject] public IDependencyGraphApiService DependencyGraphApiService { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await Load();
    }

    private async Task Load()
    {
        PageError.Reset();
        var callResponse = await AdminApiService.GetEnvironments();
        if (callResponse.IsSuccess && callResponse.Response != null)
            List = Mappers.EnvironmentMapper.ToClient(callResponse.Response.Environments);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task Save()
    {
        var callResponse = await AdminApiService.SaveEnvironments(List);
        if (!callResponse.IsSuccess)
            PageError.OnError("Error saving data, please try again", new Exception());
        else
        {
            await Load();
            PageError.Reset();
        }
    }

    private void AddItem()
    {
        List.Add(new ConfigEnvironment());
    }
    
    private async Task LoadUsages()
    {
        try
        {
            var callResponse = await DependencyGraphApiService.GetDependencyGraph();

            if (!callResponse.IsSuccess)
            {
                PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
                return;
            }

            if (callResponse.Response is not {})
            {
                PageError.OnError("Response was empty", new Exception());
                return;
            }

            foreach (var environment in List)
            {
                environment.Usages.Clear();
                var usages = callResponse.Response.Edges.Where(e => e.ToId == environment.Id);
                environment.Usages.AddRange(usages);
            }
            
            _usageLoaded = true;
        }
        catch (Exception ex)
        {
            PageError.OnError("Error loading usages", ex);
        }
    }
    
    private void GoToUsage(string id)
    {
        NavigationManager.NavigateTo($"EditConfiguration/{id}", new NavigationOptions { ForceLoad = false });
    }
}