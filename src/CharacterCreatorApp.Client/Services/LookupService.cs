using System.Globalization;

using CharacterCreatorApp.Client.Services.Http;
using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.Services;

/// <summary>
/// The D&amp;D reference data: races, classes, spells and the rest. None of it changes while the
/// app is open, so each list is fetched once and then served from memory; going back and forth
/// through the wizard no longer refetches every list on every step.
/// </summary>
public class LookupService(HttpClient http)
{
    private readonly HttpClient _http = http;
    private readonly Dictionary<string, object> _cache = [];

    /// <summary>The books of a rules edition a character may be allowed, its Player's Handbook first.</summary>
    public Task<List<BookApiResponse>> GetBooksAsync(string edition) =>
        GetCachedAsync<List<BookApiResponse>>($"/api/books{ToQueryString(new() { ["edition"] = edition })}");

    /// <summary>
    /// The races of one rules edition, "2014" or "2024", or of both when it is null; only those
    /// (and their subraces) from the given books, when there are any.
    /// </summary>
    public Task<List<RaceApiResponse>> GetRacesAsync(string? edition, IEnumerable<string>? sources = null) =>
        GetCachedAsync<List<RaceApiResponse>>($"/api/races/get-list{ToQueryString(new() { ["edition"] = edition }, sources)}");

    /// <summary>The classes of one rules edition, "2014" or "2024", or of both when it is null; only those from the given books, when there are any.</summary>
    public Task<List<ClassApiResponse>> GetClassesAsync(string? edition, IEnumerable<string>? sources = null) =>
        GetCachedAsync<List<ClassApiResponse>>($"/api/classes/get-list{ToQueryString(new() { ["edition"] = edition }, sources)}");

    public Task<ClassApiResponse?> GetClassByIdAsync(string id) => GetCachedOrNullAsync<ClassApiResponse>($"/api/classes/{id}");

    public Task<List<SubclassApiResponse>> GetSubclassesByClassAsync(string classId, IEnumerable<string>? sources = null) =>
        GetCachedAsync<List<SubclassApiResponse>>($"/api/subclasses{ToQueryString(new() { ["classId"] = classId }, sources)}");

    public Task<SubclassApiResponse?> GetSubclassByIdAsync(string id) => GetCachedOrNullAsync<SubclassApiResponse>($"/api/subclasses/{id}");

    /// <summary>The backgrounds of one rules edition, "2014" or "2024", or of both when it is null; only those from the given books, when there are any.</summary>
    public Task<List<BackgroundApiResponse>> GetBackgroundsAsync(string? edition, IEnumerable<string>? sources = null) =>
        GetCachedAsync<List<BackgroundApiResponse>>($"/api/backgrounds{ToQueryString(new() { ["edition"] = edition }, sources)}");

    public Task<BackgroundApiResponse?> GetBackgroundByIdAsync(string id) => GetCachedOrNullAsync<BackgroundApiResponse>($"/api/backgrounds/{id}");

    public Task<List<SpellApiResponse>> GetSpellsAsync(
        string? classId = null,
        string? subclassId = null,
        int? level = null,
        int? maxLevel = null,
        string? school = null,
        bool? isRitual = null,
        IEnumerable<string>? sources = null)
    {
        var query = ToQueryString(new Dictionary<string, string?>
        {
            ["classId"] = classId,
            ["subclassId"] = subclassId,
            ["level"] = level?.ToString(CultureInfo.InvariantCulture),
            ["maxLevel"] = maxLevel?.ToString(CultureInfo.InvariantCulture),
            ["school"] = school,
            ["isRitual"] = isRitual?.ToString()
        }, sources);

        return GetCachedAsync<List<SpellApiResponse>>($"/api/spells{query}");
    }

    /// <summary>The feats of one rules edition, "2014" or "2024", or of both when it is null; only those from the given books, when there are any.</summary>
    public Task<List<FeatApiResponse>> GetFeatsAsync(string? edition, IEnumerable<string>? sources = null) =>
        GetCachedAsync<List<FeatApiResponse>>($"/api/feats{ToQueryString(new() { ["edition"] = edition }, sources)}");

    public Task<ProficiencyApiResponse?> GetProficiencyByIdAsync(string id) => GetCachedOrNullAsync<ProficiencyApiResponse>($"/api/proficiencies/{id}");

    public async Task<List<ProficiencyApiResponse>> GetSkillProficienciesAsync()
    {
        // The data spells the type "skill"; this compared with "Skill", found nothing, and left
        // a half-elf no skills to pick
        var all = await GetCachedAsync<List<ProficiencyApiResponse>>("/api/proficiencies");
        return all.Where(p => string.Equals(p.Type, "skill", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>The languages a character of this rules edition may pick.</summary>
    public Task<List<LanguageApiResponse>> GetLanguagesAsync(string edition) =>
        GetCachedAsync<List<LanguageApiResponse>>($"/api/languages/get-list{ToQueryString(new() { ["edition"] = edition })}");

    private async Task<T> GetCachedAsync<T>(string url)
        where T : class
    {
        if (_cache.TryGetValue(url, out var cached))
        {
            return (T)cached;
        }

        var response = await _http.GetAsync(url);
        var value = await response.ReadAsync<T>();

        _cache[url] = value;
        return value;
    }

    private async Task<T?> GetCachedOrNullAsync<T>(string url)
        where T : class
    {
        if (_cache.TryGetValue(url, out var cached))
        {
            return (T)cached;
        }

        var response = await _http.GetAsync(url);
        var value = await response.ReadOrNullIfNotFoundAsync<T>();

        if (value is not null)
        {
            _cache[url] = value;
        }

        return value;
    }

    // "?a=1&b=2&sources=XPHB&sources=EFA" from the parameters that have a value and the books,
    // or "" when there are none. Written out rather than taken from ASP.NET Core's QueryString,
    // which would pull a 2.x server package into the WebAssembly download for one method.
    private static string ToQueryString(Dictionary<string, string?> parameters, IEnumerable<string>? sources = null)
    {
        var pairs = parameters
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}")
            .Concat((sources ?? []).Select(source => $"sources={Uri.EscapeDataString(source)}"))
            .ToList();

        return pairs.Count == 0 ? string.Empty : "?" + string.Join('&', pairs);
    }
}
