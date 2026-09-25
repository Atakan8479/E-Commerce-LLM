import {
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
  InventoryActionStore
} from '../../flash-sale/inventory-action.store';

import {
  RecommendationPollingStore
} from '../../recommendations/recommendation-polling.store';

import {
  CatalogStore
} from '../catalog.store';

import {
  CatalogPage
} from './catalog-page';

describe(
  'CatalogPage',
  () => {
    let ensureLoaded:
      ReturnType<typeof vi.fn>;

    let reload:
      ReturnType<typeof vi.fn>;

    let decreaseStock:
      ReturnType<typeof vi.fn>;

    let retry:
      ReturnType<typeof vi.fn>;

    beforeEach(
      () => {
        ensureLoaded =
          vi.fn();

        reload =
          vi.fn();

        decreaseStock =
          vi.fn();

        retry =
          vi.fn();

        TestBed.configureTestingModule({
          imports: [
            CatalogPage
          ],
          providers: [
            {
              provide:
                CatalogStore,
              useValue: {
                ensureLoaded,
                reload,
                isLoading:
                  vi.fn(() => false),
                hasItems:
                  vi.fn(() => false),
                hasError:
                  vi.fn(() => false),
                isEmpty:
                  vi.fn(() => false),
                errorMessage:
                  vi.fn(() => null),
                items:
                  vi.fn(() => [])
              }
            },
            {
              provide:
                InventoryActionStore,
              useValue: {
                decreaseStock,
                isSubmitting:
                  vi.fn(() => false),
                feedbackFor:
                  vi.fn(() => null)
              }
            },
            {
              provide:
                RecommendationPollingStore,
              useValue: {
                retry,
                hasActivity:
                  vi.fn(() => false),
                stateFor:
                  vi.fn()
              }
            }
          ]
        });
      }
    );

    it(
      'loads the catalog during initialization',
      () => {
        const fixture =
          TestBed.createComponent(
            CatalogPage
          );

        fixture.detectChanges();

        expect(
          ensureLoaded
        ).toHaveBeenCalledTimes(1);
      }
    );

    it(
      'reloads the catalog when refresh is requested',
      () => {
        const fixture =
          TestBed.createComponent(
            CatalogPage
          );

        fixture.componentInstance.refresh();

        expect(
          reload
        ).toHaveBeenCalledTimes(1);
      }
    );

    it(
      'delegates stock decrease intent to InventoryActionStore',
      () => {
        const fixture =
          TestBed.createComponent(
            CatalogPage
          );

        fixture.componentInstance
          .decreaseStock({
            productId:
              'product-1',
            quantity: 2
          });

        expect(
          decreaseStock
        ).toHaveBeenCalledWith(
          'product-1',
          2
        );
      }
    );

    it(
      'delegates recommendation retry to RecommendationPollingStore',
      () => {
        const fixture =
          TestBed.createComponent(
            CatalogPage
          );

        fixture.componentInstance
          .retryRecommendations(
            'product-1'
          );

        expect(
          retry
        ).toHaveBeenCalledWith(
          'product-1'
        );
      }
    );
  }
);