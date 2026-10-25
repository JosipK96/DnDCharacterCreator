using System.Net.Http.Json;

using CharacterCreatorApp.Client.Services.Http;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.Services;

public class CharacterService(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<Guid> FinalizeCharacterAsync(Guid draftId)
    {
        var response = await _http.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId));
        return await response.ReadAsync<Guid>();
    }

    public async Task<FilterResponse<CharacterSummaryApiResponse>> GetMyCharactersAsync(
        int pageIndex = 0,
        int pageSize = 12,
        string? sortColumn = null,
        string? sortDirection = null,
        string? classId = null,
        string? searchTerm = null)
    {
        var filterRequest = new FilterRequest<GetCharactersApiRequest>
        {
            SearchParams = new GetCharactersApiRequest
            {
                Name = searchTerm,
                ClassId = classId
            },
            Sorting = !string.IsNullOrWhiteSpace(sortColumn)
                ? new SortingParams
                {
                    SortColumn = sortColumn,
                    SortDirection = sortDirection?.ToUpperInvariant()
                }
                : null,
            Pagination = new PaginationParams
            {
                PageIndex = pageIndex,
                PageSize = pageSize
            }
        };

        var response = await _http.PostAsJsonAsync("/api/characters/get-list", filterRequest);
        return await response.ReadAsync<FilterResponse<CharacterSummaryApiResponse>>();
    }

    /// <summary>The character, or null if it does not exist or belongs to someone else.</summary>
    public async Task<CharacterDetailsApiResponse?> GetCharacterDetailsAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/characters/{id}");
        return await response.ReadOrNullIfNotFoundAsync<CharacterDetailsApiResponse>();
    }

    /// <summary>
    /// Opens the character as a draft with every choice it was made with; returns the draft's id
    /// for the wizard. Finalizing the draft replaces the character.
    /// </summary>
    public async Task<Guid> StartEditAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/characters/{id}/edit", null);
        return await response.ReadAsync<Guid>();
    }

    /// <summary>
    /// Opens the character one level up as a draft that keeps every pick; returns the draft's id
    /// for the wizard, which asks only what the new level brings. Finalizing it replaces the character.
    /// </summary>
    public async Task<Guid> StartLevelUpAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/characters/{id}/level-up", null);
        return await response.ReadAsync<Guid>();
    }

    /// <summary>
    /// The character sheet as a PDF, laid out for A4 or, with paper "letter", US Letter; the file
    /// name is the one the API gives it.
    /// </summary>
    public async Task<DownloadedFile> GetCharacterSheetPdfAsync(Guid id, string paper)
    {
        var response = await _http.GetAsync($"/api/characters/{id}/sheet.pdf?paper={Uri.EscapeDataString(paper)}");
        await response.EnsureSuccessAsync();

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "character.pdf";

        return new DownloadedFile(fileName, await response.Content.ReadAsByteArrayAsync());
    }

    public async Task DeleteCharacterAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/characters/{id}");
        await response.EnsureSuccessAsync();
    }
}
