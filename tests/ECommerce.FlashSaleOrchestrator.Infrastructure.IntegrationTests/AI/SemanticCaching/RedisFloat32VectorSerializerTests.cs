using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.SemanticCaching;

public sealed class
    RedisFloat32VectorSerializerTests
{
    [Fact]
    public void Serialize_ShouldProduceExpectedByteLength()
    {
        var values =
            Enumerable.Range(
                    0,
                    1024)
                .Select(
                    index =>
                        (float)index)
                .ToArray();

        var embedding =
            new SemanticRecommendationEmbedding(
                values);

        var bytes =
            RedisFloat32VectorSerializer.Serialize(
                embedding,
                1024);

        Assert.Equal(
            4096,
            bytes.Length);
    }

    [Fact]
    public void Serialize_ShouldPreserveFloatValues()
    {
        var embedding =
            new SemanticRecommendationEmbedding(
                new float[]
                {
                    1.0f,
                    0.5f,
                    -2.0f
                });

        var bytes =
            RedisFloat32VectorSerializer.Serialize(
                embedding,
                3);

        var values =
            new float[3];

        Buffer.BlockCopy(
            bytes,
            0,
            values,
            0,
            bytes.Length);

        Assert.Equal(
            1.0f,
            values[0]);

        Assert.Equal(
            0.5f,
            values[1]);

        Assert.Equal(
            -2.0f,
            values[2]);
    }

    [Fact]
    public void Serialize_ShouldRejectDimensionMismatch()
    {
        var embedding =
            new SemanticRecommendationEmbedding(
                new float[]
                {
                    0.1f,
                    0.2f,
                    0.3f
                });

        Assert.Throws<InvalidOperationException>(
            () =>
                RedisFloat32VectorSerializer.Serialize(
                    embedding,
                    1024));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Serialize_ShouldRejectInvalidExpectedDimensions(
        int expectedDimensions)
    {
        var embedding =
            new SemanticRecommendationEmbedding(
                new float[]
                {
                    0.1f
                });

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                RedisFloat32VectorSerializer.Serialize(
                    embedding,
                    expectedDimensions));
    }
}