using System.Globalization;
using System.Threading.RateLimiting;

using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CharacterCreatorApp.Api.Infrastructure.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>The policy sign-in and registration share, so one address has one allowance for both.</summary>
    public const string AuthPolicy = "auth";

    public const string TooManyRequestsCode = "RateLimit.TooManyRequests";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingSettings>().BindConfiguration(RateLimitingSettings.SectionName);

        // Configured through IOptions so the limit is read when the first request arrives, from
        // the configuration the host ended up with
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitingSettings>>((limiter, settings) =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Partitioned by the address the forwarded headers middleware resolved. Null (no
            // address at all) shares one bucket, which fails closed.
            limiter.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.Value.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.Value.WindowSeconds),
                    QueueLimit = 0
                }));
            limiter.OnRejected = WriteTooManyRequestsAsync;
        });

        services.AddRateLimiter(_ => { });

        return services;
    }

    private static async ValueTask WriteTooManyRequestsAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problem = (CustomProblemDetails)http.RequestServices.GetRequiredService<ProblemDetailsFactory>().CreateProblemDetails(
            http,
            StatusCodes.Status429TooManyRequests,
            detail: "Too many attempts. Please wait a minute and try again.",
            instance: http.Request.Path);
        problem.Errors.Add(ProblemDetailsError.Create(TooManyRequestsCode, detail: problem.Detail));

        await http.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}
