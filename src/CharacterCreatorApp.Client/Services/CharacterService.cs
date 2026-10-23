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

    public async Task DeleteCharacterAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/characters/{id}");
        await response.EnsureSuccessAsync();
    }
}
