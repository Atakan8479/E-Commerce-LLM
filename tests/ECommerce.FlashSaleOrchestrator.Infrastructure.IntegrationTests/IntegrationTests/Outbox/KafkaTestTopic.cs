using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Outbox;

internal sealed class KafkaTestTopic
    : IAsyncDisposable
{
    private readonly IAdminClient _adminClient;

    private KafkaTestTopic(
        IAdminClient adminClient,
        string bootstrapServers,
        string name)
    {
        _adminClient =
            adminClient;

        BootstrapServers =
            bootstrapServers;

        Name =
            name;
    }

    public string BootstrapServers { get; }

    public string Name { get; }

    public static async Task<KafkaTestTopic> CreateAsync()
    {
        var bootstrapServers =
            Environment.GetEnvironmentVariable(
                "FLASHSALE_KAFKA_BOOTSTRAP_SERVERS");

        if (string.IsNullOrWhiteSpace(
            bootstrapServers))
        {
            bootstrapServers =
                "localhost:19092";
        }

        var topicName =
            $"inventory.stock-depleted.tests.{Guid.NewGuid():N}";

        var adminClient =
            new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers =
                        bootstrapServers
                })
                .Build();

        try
        {
            await adminClient.CreateTopicsAsync(
                new[]
                {
                    new TopicSpecification
                    {
                        Name =
                            topicName,

                        NumPartitions =
                            1,

                        ReplicationFactor =
                            1
                    }
                });

            return new KafkaTestTopic(
                adminClient,
                bootstrapServers,
                topicName);
        }
        catch
        {
            adminClient.Dispose();

            throw;
        }
    }

    public IConsumer<string, string> CreateConsumer()
    {
        var consumer =
            new ConsumerBuilder<string, string>(
                new ConsumerConfig
                {
                    BootstrapServers =
                        BootstrapServers,

                    GroupId =
                        $"flashsale-outbox-tests-{Guid.NewGuid():N}",

                    AutoOffsetReset =
                        AutoOffsetReset.Earliest,

                    EnableAutoCommit =
                        false
                })
                .Build();

        consumer.Subscribe(
            Name);

        return consumer;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _adminClient.DeleteTopicsAsync(
                new[]
                {
                    Name
                });
        }
        catch (DeleteTopicsException)
        {
            // Best-effort cleanup for isolated test topics.
        }
        finally
        {
            _adminClient.Dispose();
        }
    }
}