namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;

public interface ICorrelationContext
{
    string CorrelationId { get; }

    void SetCorrelationId(
        string correlationId);
}