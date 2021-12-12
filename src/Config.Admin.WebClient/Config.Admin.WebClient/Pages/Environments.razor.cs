using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class Environments
{
    private readonly List<string> _deletedList = new();
    public List<ConfigEnvironment> List { get; set; } = new();
    [Inject] public IAdminApiService AdminApiService { get; set; } = null!;
    //[CascadingParameter] public PageError PageError { get; set; } = null!;
    [CascadingParameter] public Action<string, Exception> OnError { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    { 
        var callResponse = await AdminApiService.GetEnvironments();
        if (callResponse.IsSuccess && callResponse.Response != null)
            List = Mappers.EnvironmentMapper.ToClient(callResponse.Response.Environments);
        else
        {
            OnError(!string.IsNullOrWhiteSpace(callResponse.ErrorMessage) ? callResponse.ErrorMessage : callResponse.Exception.Message, null);
           
            //PageError = new PageError
            //{
            //    IsError = true,
            //    ErrorMessage = !string.IsNullOrWhiteSpace(callResponse.ErrorMessage) ? callResponse.ErrorMessage : callResponse.Exception.Message
            //};
        }
    }

    private async Task Save()
    {
        await AdminApiService.SaveEnvironments(Mappers.EnvironmentMapper.ToApi(List), _deletedList);
    }

    private void AddItem()
    {
        List.Add(new ConfigEnvironment { Name = "[new item]" });
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