using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Environments
{
    public List<Model.Environment> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; }

    private Model.Environment selectedItem;

    protected override async Task OnInitializedAsync()
    {
        var response = await AdminApiService.GetEnvironments();
        List = Mappers.EnvironmentMapper.ToClient(response.Environments);
    }

    private async Task Save()
    {
        await AdminApiService.SaveEnvironments(Mappers.EnvironmentMapper.ToApi(List));
    }
}