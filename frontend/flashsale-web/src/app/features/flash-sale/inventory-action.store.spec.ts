import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  TestBed
} from '@angular/core/testing';

import {
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
  InventoryApiService
} from '../../core/api/inventory-api.service';

import {
  CatalogStore
} from '../catalog/catalog.store';

import {
  RecommendationPollingStore
} from '../recommendations/recommendation-polling.store';

import {
  InventoryActionStore
} from './inventory-action.store';

describe(
  'InventoryActionStore',
  () => {
    const productId =
      '22222222-2222-2222-2222-222222222222';

    let store:
      InventoryActionStore;

    let decreaseStock:
      ReturnType<typeof vi.fn>;

    let applyInventoryUpdate:
      ReturnType<typeof vi.fn>;

    let reloadCatalog:
      ReturnType<typeof vi.fn>;

    let startRecommendationPolling:
      ReturnType<typeof vi.fn>;

    beforeEach(
      () => {
        decreaseStock =
          vi.fn();

        applyInventoryUpdate =
          vi.fn();

        reloadCatalog =
          vi.fn();

        startRecommendationPolling =
          vi.fn();

        TestBed.configureTestingModule({
          providers: [
            InventoryActionStore,
            {
              provide: InventoryApiService,
              useValue: {
                decreaseStock
              }
            },
            {
              provide: CatalogStore,
              useValue: {
                applyInventoryUpdate,
                reload:
                  reloadCatalog
              }
            },
            {
              provide:
                RecommendationPollingStore,
              useValue: {
                start:
                  startRecommendationPolling
              }
            }
          ]
        });

        store =
          TestBed.inject(
            InventoryActionStore
          );
      }
    );

    it(
      'updates inventory and reloads the catalog after a successful decrease',
      () => {
        decreaseStock.mockReturnValue(
          of({
            productId,
            remainingQuantity: 4,
            isDepleted: false,
            recommendationRequested:
              false,
            correlationId:
              'correlation-1'
          })
        );

        store.decreaseStock(
          productId,
          1
        );

        expect(
          applyInventoryUpdate
        ).toHaveBeenCalledWith(
          productId,
          4,
          false
        );

        expect(
          reloadCatalog
        ).toHaveBeenCalledTimes(1);

        expect(
          startRecommendationPolling
        ).not.toHaveBeenCalled();

        expect(
          store.feedbackFor(
            productId
          )
        ).toEqual({
          kind: 'success',
          message:
            'Stock updated successfully. ' +
            '4 items remaining.',
          errorCode: null,
          correlationId:
            'correlation-1',
          result: {
            productId,
            remainingQuantity: 4,
            isDepleted: false,
            recommendationRequested:
              false,
            correlationId:
              'correlation-1'
          }
        });
      }
    );

    it(
      'starts recommendation polling after stock depletion',
      () => {
        decreaseStock.mockReturnValue(
          of({
            productId,
            remainingQuantity: 0,
            isDepleted: true,
            recommendationRequested:
              true,
            correlationId:
              'workflow-correlation'
          })
        );

        store.decreaseStock(
          productId,
          1
        );

        expect(
          applyInventoryUpdate
        ).toHaveBeenCalledWith(
          productId,
          0,
          true
        );

        expect(
          startRecommendationPolling
        ).toHaveBeenCalledWith(
          productId,
          'workflow-correlation'
        );

        expect(
          store.feedbackFor(
            productId
          )?.message
        ).toBe(
          'Stock depleted. ' +
          'The recommendation workflow was requested.'
        );
      }
    );

    it(
      'does not call the API for an invalid quantity',
      () => {
        store.decreaseStock(
          productId,
          0
        );

        store.decreaseStock(
          productId,
          1.5
        );

        expect(
          decreaseStock
        ).not.toHaveBeenCalled();
      }
    );

    it(
      'surfaces insufficient-stock ProblemDetails and reconciles the catalog',
      () => {
        decreaseStock.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 409,
                error: {
                  title:
                    'Insufficient stock.',
                  detail:
                    'The requested quantity is not available.',
                  errorCode:
                    'insufficient-stock',
                  correlationId:
                    'conflict-correlation'
                }
              })
          )
        );

        store.decreaseStock(
          productId,
          10
        );

        expect(
          reloadCatalog
        ).toHaveBeenCalledTimes(1);

        expect(
          store.feedbackFor(
            productId
          )
        ).toEqual({
          kind: 'error',
          message:
            'The requested quantity is not available.',
          errorCode:
            'insufficient-stock',
          correlationId:
            'conflict-correlation',
          result: null
        });
      }
    );

    it(
      'reconciles the catalog after inventory concurrency conflict',
      () => {
        decreaseStock.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 409,
                error: {
                  detail:
                    'Inventory changed while this request was being processed.',
                  errorCode:
                    'inventory-concurrency-conflict',
                  correlationId:
                    'concurrency-correlation'
                }
              })
          )
        );

        store.decreaseStock(
          productId,
          1
        );

        expect(
          reloadCatalog
        ).toHaveBeenCalledTimes(1);

        expect(
          store.feedbackFor(
            productId
          )?.errorCode
        ).toBe(
          'inventory-concurrency-conflict'
        );
      }
    );

    it(
      'reconciles the catalog after inventory item not found',
      () => {
        decreaseStock.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 404,
                error: {
                  detail:
                    'Inventory item was not found.',
                  errorCode:
                    'inventory-item-not-found',
                  correlationId:
                    'not-found-correlation'
                }
              })
          )
        );

        store.decreaseStock(
          productId,
          1
        );

        expect(
          reloadCatalog
        ).toHaveBeenCalledTimes(1);

        expect(
          store.feedbackFor(
            productId
          )?.errorCode
        ).toBe(
          'inventory-item-not-found'
        );
      }
    );

    it(
      'uses validation errors from the API response',
      () => {
        decreaseStock.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 400,
                error: {
                  title:
                    'Validation failed.',
                  errorCode:
                    'validation-failed',
                  correlationId:
                    'validation-correlation',
                  errors: {
                    Quantity: [
                      'Quantity must be greater than zero.'
                    ]
                  }
                }
              })
          )
        );

        store.decreaseStock(
          productId,
          1
        );

        expect(
          store.feedbackFor(
            productId
          )?.message
        ).toBe(
          'Quantity must be greater than zero.'
        );

        expect(
          reloadCatalog
        ).not.toHaveBeenCalled();
      }
    );

    it(
      'surfaces API connectivity failure without reloading the catalog',
      () => {
        decreaseStock.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 0,
                statusText:
                  'Unknown Error'
              })
          )
        );

        store.decreaseStock(
          productId,
          1
        );

        expect(
          store.feedbackFor(
            productId
          )?.message
        ).toBe(
          'The API could not be reached. ' +
          'Verify that the backend is running.'
        );

        expect(
          reloadCatalog
        ).not.toHaveBeenCalled();
      }
    );
  }
);