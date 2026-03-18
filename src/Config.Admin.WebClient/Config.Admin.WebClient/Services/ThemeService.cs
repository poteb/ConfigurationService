using Microsoft.JSInterop;

namespace pote.Config.Admin.WebClient.Services;

public enum ThemeChoice
{
    System,
    Light,
    Dark
}

public class ThemeService
{
    private const string StorageKey = "theme-preference";
    private readonly IJSRuntime _js;
    private ThemeChoice _choice = ThemeChoice.System;
    private bool _systemPrefersDark;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public event Action? OnChange;

    public ThemeChoice Choice => _choice;

    public bool IsDarkMode => _choice switch
    {
        ThemeChoice.Dark => true,
        ThemeChoice.Light => false,
        _ => _systemPrefersDark
    };

    public async Task InitializeAsync()
    {
        var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (Enum.TryParse<ThemeChoice>(stored, true, out var parsed))
            _choice = parsed;

        _systemPrefersDark = await _js.InvokeAsync<bool>("eval",
            "window.matchMedia('(prefers-color-scheme: dark)').matches");

        await ApplyBodyClassAsync();
    }

    public async Task SetThemeAsync(ThemeChoice choice)
    {
        _choice = choice;
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, choice.ToString());
        await ApplyBodyClassAsync();
        OnChange?.Invoke();
    }

    public async Task ApplyBodyClassAsync()
    {
        if (IsDarkMode)
            await _js.InvokeVoidAsync("eval", "document.body.classList.add('app-dark')");
        else
            await _js.InvokeVoidAsync("eval", "document.body.classList.remove('app-dark')");
    }
}
