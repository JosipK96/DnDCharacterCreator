using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Contracts.Common;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping;

public static class FilterRequestMappingExtensions
{
    /// <summary>
    /// Maps the wire filter onto the application's own, falling back to the default page when
    /// the client sent no body at all.
    /// </summary>
    public static FilterOptions<TDest> ToFilterOptions<TSource, TDest>(this FilterRequest<TSource>? sourceRequest)
        where TSource : new()
        where TDest : new()
    {
        return sourceRequest != null
            ? sourceRequest.Adapt<FilterOptions<TDest>>()
            : FilterOptions<TDest>.Default;
    }
}
