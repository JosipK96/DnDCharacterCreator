using CharacterCreatorApp.Infrastructure.Seed.Seeders;

using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Infrastructure.Seed
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSeeders(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromAssemblyOf<ApplicationDbContext>()
                .AddClasses(classes => classes.AssignableTo<IEntitySeeder>())
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            );

            return services;
        }
    }
}
