import {
  ComponentFixture,
  TestBed
} from '@angular/core/testing';

import {
  beforeEach,
  describe,
  expect,
  it,
  vi
} from 'vitest';

import {
  CatalogItem
} from '../../../core/models/catalog.models';

import {
  ProductCard
} from './product-card';

describe(
  'ProductCard',
  () => {
    let fixture:
      ComponentFixture<ProductCard>;

    let component:
      ProductCard;

    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              ProductCard
            ]
          })
          .compileComponents();

        fixture =
          TestBed.createComponent(
            ProductCard
          );

        component =
          fixture.componentInstance;

        fixture.componentRef.setInput(
          'item',
          createItem()
        );

        fixture.detectChanges();
      }
    );

    it(
      'renders product and inventory information',
      () => {
        const element =
          fixture.nativeElement as HTMLElement;

        expect(
          element.textContent
        ).toContain(
          'Wireless Gaming Mouse'
        );

        expect(
          element.textContent
        ).toContain(
          '5 in stock'
        );

        expect(
          element.textContent
        ).toContain(
          'gaming-mouse'
        );
      }
    );

    it(
      'allows a valid stock decrease',
      () => {
        expect(
          component.canSubmit()
        ).toBe(true);
      }
    );

    it(
      'rejects quantity greater than available inventory',
      () => {
        component.quantity.set(6);

        fixture.detectChanges();

        expect(
          component.canSubmit()
        ).toBe(false);

        expect(
          fixture.nativeElement.textContent
        ).toContain(
          'Quantity cannot exceed the available inventory.'
        );
      }
    );

    it(
      'rejects non-integer quantity input',
      () => {
        const input =
          fixture.nativeElement.querySelector(
            'input'
          ) as HTMLInputElement;

        input.value = '1.5';

        input.dispatchEvent(
          new Event('input')
        );

        fixture.detectChanges();

        expect(
          component.quantity()
        ).toBe(0);

        expect(
          component.canSubmit()
        ).toBe(false);
      }
    );

    it(
      'emits a decrease intent for a valid submission',
      () => {
        const emitted =
          vi.fn();

        component.decreaseStock.subscribe(
          emitted
        );

        component.quantity.set(2);

        component.submitDecrease();

        expect(
          emitted
        ).toHaveBeenCalledWith({
          productId:
            'product-1',
          quantity: 2
        });
      }
    );

    it(
      'does not emit when submission is invalid',
      () => {
        const emitted =
          vi.fn();

        component.decreaseStock.subscribe(
          emitted
        );

        component.quantity.set(10);

        component.submitDecrease();

        expect(
          emitted
        ).not.toHaveBeenCalled();
      }
    );

    it(
      'disables stock changes for depleted items',
      () => {
        fixture.componentRef.setInput(
          'item',
          createItem({
            availableQuantity: 0,
            isDepleted: true
          })
        );

        fixture.detectChanges();

        expect(
          component.stockLabel()
        ).toBe('Out of stock');

        expect(
          component.canSubmit()
        ).toBe(false);

        const button =
          fixture.nativeElement.querySelector(
            'button'
          ) as HTMLButtonElement;

        expect(
          button.disabled
        ).toBe(true);
      }
    );

    it(
      'renders backend error feedback and correlation id',
      () => {
        fixture.componentRef.setInput(
          'feedback',
          {
            kind: 'error',
            message:
              'The requested quantity is not available.',
            errorCode:
              'insufficient-stock',
            correlationId:
              'conflict-correlation',
            result: null
          }
        );

        fixture.detectChanges();

        const element =
          fixture.nativeElement as HTMLElement;

        expect(
          element.textContent
        ).toContain(
          'Update failed'
        );

        expect(
          element.textContent
        ).toContain(
          'insufficient-stock'
        );

        expect(
          element.textContent
        ).toContain(
          'conflict-correlation'
        );
      }
    );
  }
);

function createItem(
  overrides:
    Partial<CatalogItem> = {}
): CatalogItem {
  return {
    productId: 'product-1',
    name:
      'Wireless Gaming Mouse',
    category:
      'gaming-mouse',
    availableQuantity: 5,
    isDepleted: false,
    ...overrides
  };
}