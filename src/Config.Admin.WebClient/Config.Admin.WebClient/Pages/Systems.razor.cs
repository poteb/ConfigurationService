using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Systems
{
    public List<Model.System> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var response = await AdminApiService.GetSystems();
        List = Mappers.SystemMapper.ToClient(response.Systems);
    }
}