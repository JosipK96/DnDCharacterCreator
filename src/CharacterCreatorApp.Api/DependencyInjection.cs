using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

using CharacterCreatorApp.Api.Helpers.Jwt;
using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Infrastructure.Authentication;
using CharacterCreatorApp.Api.Infrastructure.ExceptionHandling;
using CharacterCreatorApp.Api.Infrastructure.RateLimiting;
using CharacterCreatorApp.Api.Infrastructure.Registration;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace CharacterCreatorApp.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddEndpointsApiExplorer();

            services.AddMappings();
            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, UserContext>();
            services.AddCustomProblemDetails();

            services.AddScoped<IJwtHelper, JwtHelper>();

            services.AddOptions<RegistrationSettings>().BindConfiguration(RegistrationSettings.SectionName);
            services.AddScoped<IInviteCodeGate, InviteCodeGate>();
            services.AddAuthRateLimiting();

            return services;
        }

        public static IServiceCollection AddJwtAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection(JwtSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Options validation runs on start, but the bearer handler is configured before
            // that, so read the section here too and fail with the same message.
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    $"The '{JwtSettings.SectionName}' configuration section is missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Key is not configured. Set it with " +
                    "'dotnet user-secrets set \"JwtSettings:Key\" \"<value>\"' or the " +
                    "JwtSettings__Key environment variable.");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = "role"
                };
            });

            services.AddAuthorization();

            return services;
        }

        private static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddSingleton<ProblemDetailsFactory, CustomProblemDetailsFactory>();
            return services;
        }
    }
}
