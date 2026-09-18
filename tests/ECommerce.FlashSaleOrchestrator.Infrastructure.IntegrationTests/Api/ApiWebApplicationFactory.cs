using ECommerce.FlashSaleOrchestrator.Api
    .BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;

internal sealed class ApiWebApplicationFactory
    : WebApplicationFactory<global::Program>
{
    private readonly string _connectionString;

    public ApiWebApplicationFactory(
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        _connectionString =
            connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");

        builder.UseSetting(
            "FLASHSALE_SQL_CONNECTION",
            _connectionString);

        builder.ConfigureServices(
            services =>
            {
                var outboxPublisherDescriptor =
                    services.SingleOrDefault(
                        descriptor =>
                            descriptor.ServiceType ==
                            typeof(IHostedService) &&
                            descriptor.ImplementationType ==
                            typeof(OutboxPublisherWorker));

                if (outboxPublisherDescriptor is not null)
                {
                    services.Remove(
                        outboxPublisherDescriptor);
                }
            });
    }
}