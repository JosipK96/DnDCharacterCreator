using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetFeatEligibility;

/// <summary>
/// Which feats of the draft's edition and books its character can take. Increases are the ability
/// points the improvements step has spent so far, one ability per point ("strength" twice for +2),
/// which an ability minimum counts for a feat taken for an improvement.
/// </summary>
public record GetFeatEligibilityQuery(Guid DraftId, List<string> Increases) : IQuery<Response<List<FeatEligibilityReadModel>>>;
