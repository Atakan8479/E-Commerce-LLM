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
  CatalogStore
} from '../catalog.store';

import {
  ProductCard
} from '../product-card/product-card';

@Component({
  selector: 'app-catalog-page',
  imports: [
    ProductCard
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
}