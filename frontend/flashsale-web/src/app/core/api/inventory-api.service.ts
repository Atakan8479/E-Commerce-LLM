import {
  inject,
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  DecreaseStockRequest,
  DecreaseStockResponse
} from '../models/inventory.models';

@Injectable({
  providedIn: 'root'
})
export class InventoryApiService {
  private readonly http =
    inject(HttpClient);

  decreaseStock(
    productId: string,
    quantity: number
  ): Observable<DecreaseStockResponse> {
    const request: DecreaseStockRequest = {
      quantity
    };

    return this.http.post<DecreaseStockResponse>(
      `/api/inventory/${encodeURIComponent(productId)}/decrease`,
      request
    );
  }
}