using Microsoft.JSInterop;

namespace CharacterCreatorApp.Client.Services.Auth;

/// <summary>Keeps the signed-in user's JWT in localStorage, so a page reload does not sign them out.</summary>
public sealed class TokenStore(IJSRuntime js)
{
    private const string Key = "authToken";

    private readonly IJSRuntime _js = js;

    public ValueTask<string?> GetAsync() => _js.InvokeAsync<string?>("localStorage.getItem", Key);

    public ValueTask SetAsync(string token) => _js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public ValueTask ClearAsync() => _js.InvokeVoidAsync("localStorage.removeItem", Key);
}
