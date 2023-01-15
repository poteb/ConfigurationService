using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Applications
{
    private bool _usageLoaded;

    private List<Application> List { get; set; } = new();
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
        var callResponse = await AdminApiService.GetApplications();
        if (callResponse.IsSuccess && callResponse.Response != null)
            List = Mappers.ApplicationMapper.ToClient(callResponse.Response.Applications);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task Save()
    {
        var callResponse = await AdminApiService.SaveApplications(List);
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
        List.Add(new Application());
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

            foreach (var application in List)
            {
                application.Usages.Clear();
                var usages = callResponse.Response.Edges.Where(e => e.ToId == application.Id);
                application.Usages.AddRange(usages);
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