using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PennyWise.Infrastructure.Data;

namespace PennyWise.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Replaces PostgreSQL with InMemory database and configures JWT for test scenarios.
/// </summary>
public class PennyWiseWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PennyWiseDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add InMemory database for testing
            services.AddDbContext<PennyWiseDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });

        builder.UseEnvironment("Development");
    }
}
