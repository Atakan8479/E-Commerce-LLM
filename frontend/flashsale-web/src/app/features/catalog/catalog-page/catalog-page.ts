import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit
} from '@angular/core';

import {
  InventoryActionStore
} from '../../flash-sale/inventory-action.store';

import {
  DecreaseStockIntent
} from '../../flash-sale/inventory-action.models';

import {
  RecommendationPollingStore
} from '../../recommendations/recommendation-polling.store';

import {
  RecommendationPanel
} from '../../recommendations/recommendation-panel/recommendation-panel';

import {
  CatalogStore
} from '../catalog.store';

import {
  ProductCard
} from '../product-card/product-card';

@Component({
  selector: 'app-catalog-page',
  imports: [
    ProductCard,
    RecommendationPanel
  ],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.css',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class CatalogPage implements OnInit {
  readonly catalog =
    inject(CatalogStore);

  readonly inventoryActions =
    inject(InventoryActionStore);

  readonly recommendations =
    inject(RecommendationPollingStore);

  ngOnInit(): void {
    this.catalog.ensureLoaded();
  }

  refresh(): void {
    this.catalog.reload();
  }

  decreaseStock(
    intent: DecreaseStockIntent
  ): void {
    this.inventoryActions.decreaseStock(
      intent.productId,
      intent.quantity
    );
  }

  retryRecommendations(
    productId: string
  ): void {
    this.recommendations.retry(
      productId
    );
  }
}