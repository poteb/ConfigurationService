using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Systems
{
    public List<ConfigSystem> List { get; set; } = new();

    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await Load();
    }

    private async Task Load()
    {
        PageError.Reset();
        var callResponse = await AdminApiService.GetSystems();
        if (callResponse.IsSuccess && callResponse.Response != null)
            List = Mappers.SystemMapper.ToClient(callResponse.Response.Systems);
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task Save()
    {
        var callResponse = await AdminApiService.SaveSystems(List);
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
        List.Add(new ConfigSystem());
    }
}