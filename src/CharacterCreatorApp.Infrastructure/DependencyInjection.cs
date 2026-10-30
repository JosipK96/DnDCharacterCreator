using CharacterCreatorApp.Application.Features.Authentication;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Infrastructure.Identity;
using CharacterCreatorApp.Infrastructure.Interceptors;
using CharacterCreatorApp.Infrastructure.Pdf;
using CharacterCreatorApp.Infrastructure.Seed;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set it with " +
                "'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\"' or the " +
                "ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());

            if (configuration.GetValue<bool>("EnableSensitiveDataLogging", false))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddRepositories();
        services.AddScoped<SeedService>();
        services.AddSingleton<ICharacterSheetPdfRenderer, CharacterSheetPdfRenderer>();
        services.AddSeeders();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationDbContext>()
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal))
                .Where(type => !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }

    public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;

            options.User.RequireUniqueEmail = true;

            // Defaults; the Identity:Lockout section overrides them (Identity__Lockout__MaxFailedAccessAttempts)
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(configuration.GetSection("Identity"));

        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}