using Microsoft.AspNetCore.Components;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;

namespace pote.Config.Admin.WebClient.Pages;

public partial class EditSettings
{
    [Inject] public ISettingsService SettingsService { get; set; } = null!;
    [CascadingParameter] public PageError PageError { get; set; } = null!;
    private Settings Settings { get; set; } = new();
    
    protected override async Task OnInitializedAsync()
    {
        await Load();
    }
    
    private async Task Load()
    {
        PageError.Reset();
        var callResponse = await SettingsService.GetSettings();
        if (callResponse is {IsSuccess: true, Response: not null})
        {
            Settings = Mappers.SettingsMapper.ToClient(callResponse.Response.Settings);
        }
        else
            PageError.OnError(callResponse.GenerateErrorMessage(), new Exception());
    }

    private async Task Save()
    {
        var callResponse = await SettingsService.SaveSettings(Settings);
        if (!callResponse.IsSuccess)
             PageError.OnError("Error saving settings data, please try again", new Exception());
        else
        {
            await Load();
            PageError.Reset();
        }
    }
}