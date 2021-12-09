using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Environments
{
    private readonly List<string> _deletedList = new();
    public List<Model.Environment> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        var response = await AdminApiService.GetEnvironments();
        List = Mappers.EnvironmentMapper.ToClient(response.Environments);
    }

    private async Task Save()
    {
        await AdminApiService.SaveEnvironments(Mappers.EnvironmentMapper.ToApi(List), _deletedList);
    }

    private void AddItem()
    {
        List.Add(new Model.Environment { Name = "[new item]" });
    }

    private void RemoveItem(string id)
    {
        Console.WriteLine(id);
        var item = List.FirstOrDefault(x => x.Id == id);
        if (item == null) return;// EventCallback.Empty;
        List.RemoveAt(List.IndexOf(item));
        _deletedList.Add(id);
        //return EventCallback.Empty;
    }
}