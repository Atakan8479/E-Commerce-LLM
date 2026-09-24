import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal
} from '@angular/core';

import {
  CatalogItem
} from '../../../core/models/catalog.models';

import {
  DecreaseStockIntent,
  InventoryActionFeedback
} from '../../flash-sale/inventory-action.models';

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

  readonly isSubmitting =
    input(false);

  readonly feedback =
    input<InventoryActionFeedback | null>(
      null
    );

  readonly decreaseStock =
    output<DecreaseStockIntent>();

  readonly quantity =
    signal(1);

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

  readonly canSubmit =
    computed(
      () => {
        const quantity =
          this.quantity();

        const item =
          this.item();

        return (
          !item.isDepleted &&
          !this.isSubmitting() &&
          Number.isInteger(quantity) &&
          quantity >= 1 &&
          quantity <=
            item.availableQuantity
        );
      }
    );

  onQuantityInput(
    event: Event
  ): void {
    const inputElement =
      event.target as HTMLInputElement;

    const parsedValue =
      Number(inputElement.value);

    this.quantity.set(
      Number.isInteger(parsedValue)
        ? parsedValue
        : 0
    );
  }

  submitDecrease(): void {
    if (!this.canSubmit()) {
      return;
    }

    this.decreaseStock.emit({
      productId:
        this.item().productId,
      quantity:
        this.quantity()
    });
  }
}