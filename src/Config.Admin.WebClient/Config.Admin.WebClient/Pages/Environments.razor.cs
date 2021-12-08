using Config.Admin.WebClient.Services;
using Microsoft.AspNetCore.Components;

namespace Config.Admin.WebClient.Pages;

public partial class Environments
{
    public List<Model.Environment> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        List = await AdminApiService.GetEnvironments();
    }
}