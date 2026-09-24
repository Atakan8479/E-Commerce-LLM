import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input
} from '@angular/core';

import {
  CatalogItem
} from '../../../core/models/catalog.models';

@Component({
  selector: 'app-product-card',
  templateUrl: './product-card.html',
  styleUrl: './product-card.css',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class ProductCard {
  readonly item =
    input.required<CatalogItem>();

  readonly stockLabel =
    computed(
      () => {
        const item =
          this.item();

        if (item.isDepleted) {
          return 'Out of stock';
        }

        return `${item.availableQuantity} in stock`;
      }
    );
}