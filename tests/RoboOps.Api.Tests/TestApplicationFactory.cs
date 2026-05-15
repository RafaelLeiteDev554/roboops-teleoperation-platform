using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoboOps.Api.Services;

namespace RoboOps.Api.Tests;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var telemetrySimulator = services
                .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(RobotTelemetrySimulator))
                .ToList();

            foreach (var descriptor in telemetrySimulator)
            {
                services.Remove(descriptor);
            }
        });
    }
}
