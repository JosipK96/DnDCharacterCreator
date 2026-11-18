using CharacterCreatorApp.Contracts.Requests.ClientError;

namespace CharacterCreatorApp.Api.Infrastructure.ErrorReporting;

public interface IClientErrorReporter
{
    void Report(ReportClientErrorRequest request);
}
