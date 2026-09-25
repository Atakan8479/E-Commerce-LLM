import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  TestBed
} from '@angular/core/testing';

import {
  Observable,
  of,
  throwError
} from 'rxjs';

import {
  beforeEach,
  describe,
  expect,
  it,
  vi
} from 'vitest';

import {
  CatalogApiService
} from '../../core/api/catalog-api.service';

import {
  CatalogResponse
} from '../../core/models/catalog.models';

import {
  CatalogStore
} from './catalog.store';

describe(
  'CatalogStore',
  () => {
    let store: CatalogStore;

    let getCatalog:
      ReturnType<typeof vi.fn>;

    beforeEach(
      () => {
        getCatalog =
          vi.fn();

        TestBed.configureTestingModule({
          providers: [
            CatalogStore,
            {
              provide: CatalogApiService,
              useValue: {
                getCatalog
              }
            }
          ]
        });

        store =
          TestBed.inject(
            CatalogStore
          );
      }
    );

    it(
      'loads the catalog and exposes the returned items',
      () => {
        getCatalog.mockReturnValue(
          of(
            createCatalogResponse()
          )
        );

        store.ensureLoaded();

        expect(
          getCatalog
        ).toHaveBeenCalledTimes(1);

        expect(
          store.isLoading()
        ).toBe(false);

        expect(
          store.hasError()
        ).toBe(false);

        expect(
          store.hasItems()
        ).toBe(true);

        expect(
          store.items()
        ).toHaveLength(2);

        expect(
          store.items()[0]
            ?.availableQuantity
        ).toBe(5);
      }
    );

    it(
      'does not load the catalog twice when ensureLoaded is called again',
      () => {
        getCatalog.mockReturnValue(
          of(
            createCatalogResponse()
          )
        );

        store.ensureLoaded();
        store.ensureLoaded();

        expect(
          getCatalog
        ).toHaveBeenCalledTimes(1);
      }
    );

    it(
      'reloads an already loaded catalog',
      () => {
        getCatalog.mockReturnValue(
          of(
            createCatalogResponse()
          )
        );

        store.ensureLoaded();
        store.reload();

        expect(
          getCatalog
        ).toHaveBeenCalledTimes(2);
      }
    );

    it(
      'applies an inventory update to the matching product',
      () => {
        getCatalog.mockReturnValue(
          of(
            createCatalogResponse()
          )
        );

        store.ensureLoaded();

        store.applyInventoryUpdate(
          'product-1',
          0,
          true
        );

        const item =
          store.items().find(
            catalogItem =>
              catalogItem.productId ===
              'product-1'
          );

        expect(
          item?.availableQuantity
        ).toBe(0);

        expect(
          item?.isDepleted
        ).toBe(true);
      }
    );

    it(
      'exposes a stable message when the API cannot be reached',
      () => {
        getCatalog.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 0,
                statusText:
                  'Unknown Error'
              })
          )
        );

        store.ensureLoaded();

        expect(
          store.hasError()
        ).toBe(true);

        expect(
          store.errorMessage()
        ).toBe(
          'The API could not be reached. ' +
          'Verify that the backend is running.'
        );

        expect(
          store.items()
        ).toEqual([]);
      }
    );

    it(
      'uses the backend ProblemDetails detail when loading fails',
      () => {
        getCatalog.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 500,
                error: {
                  detail:
                    'Catalog read failed.'
                }
              })
          )
        );

        store.ensureLoaded();

        expect(
          store.hasError()
        ).toBe(true);

        expect(
          store.errorMessage()
        ).toBe(
          'Catalog read failed.'
        );
      }
    );
  }
);

function createCatalogResponse():
  CatalogResponse {
  return {
    items: [
      {
        productId: 'product-1',
        name:
          'Wireless Gaming Mouse',
        category:
          'gaming-mouse',
        availableQuantity: 5,
        isDepleted: false
      },
      {
        productId: 'product-2',
        name:
          'Wireless Gaming Mouse Pro',
        category:
          'gaming-mouse',
        availableQuantity: 1,
        isDepleted: false
      }
    ]
  };
}