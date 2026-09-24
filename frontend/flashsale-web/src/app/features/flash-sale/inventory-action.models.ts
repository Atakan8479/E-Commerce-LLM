import {
  DecreaseStockResponse
} from '../../core/models/inventory.models';

export interface DecreaseStockIntent {
  readonly productId: string;
  readonly quantity: number;
}

export interface InventoryActionFeedback {
  readonly kind:
    | 'success'
    | 'error';

  readonly message: string;
  readonly errorCode: string | null;
  readonly correlationId: string | null;
  readonly result: DecreaseStockResponse | null;
}