using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Interfaces;
using PennyWise.Infrastructure.Data;
using PennyWise.Infrastructure.Repositories;
using PennyWise.Infrastructure.Services;

namespace PennyWise.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services into the DI container.
/// Follows the Composition Root pattern.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────
        services.AddDbContext<PennyWiseDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(PennyWiseDbContext).Assembly.FullName)));

        // ── Repositories ─────────────────────────────────────────
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ── JWT Settings & Token Service ─────────────────────────
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<ITokenService, TokenService>();

        // ── Analytics ────────────────────────────────────────────
        services.AddScoped<IPortfolioAnalyticsService, PortfolioAnalyticsService>();

        return services;
    }
}
