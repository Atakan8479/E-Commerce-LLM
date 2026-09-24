export interface DecreaseStockRequest {
  readonly quantity: number;
}

export interface DecreaseStockResponse {
  readonly productId: string;
  readonly remainingQuantity: number;
  readonly isDepleted: boolean;
  readonly recommendationRequested: boolean;
  readonly correlationId: string;
}