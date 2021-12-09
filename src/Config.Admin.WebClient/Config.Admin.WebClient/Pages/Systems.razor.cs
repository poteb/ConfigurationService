using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Systems
{
    private readonly List<string> _deletedList = new();
    public List<Model.System> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var response = await AdminApiService.GetSystems();
        List = Mappers.SystemMapper.ToClient(response.Systems);
    }

    private async Task Save()
    {
        await AdminApiService.SaveSystems(Mappers.SystemMapper.ToApi(List), _deletedList);
    }

    private void AddItem()
    {
        List.Add(new Model.System { Name = "[new item]" });
    }

    private void RemoveItem(string id)
    {
        Console.WriteLine(id);
        var item = List.FirstOrDefault(x => x.Id == id);
        if (item == null) return;
        List.RemoveAt(List.IndexOf(item));
        _deletedList.Add(id);
    }
}