import {
  provideHttpClient
} from '@angular/common/http';

import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';

import {
  TestBed
} from '@angular/core/testing';

import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it
} from 'vitest';

import {
  DecreaseStockResponse
} from '../models/inventory.models';

import {
  InventoryApiService
} from './inventory-api.service';

describe(
  'InventoryApiService',
  () => {
    let service:
      InventoryApiService;

    let httpTesting:
      HttpTestingController;

    beforeEach(
      () => {
        TestBed.configureTestingModule({
          providers: [
            provideHttpClient(),
            provideHttpClientTesting()
          ]
        });

        service =
          TestBed.inject(
            InventoryApiService
          );

        httpTesting =
          TestBed.inject(
            HttpTestingController
          );
      }
    );

    afterEach(
      () => {
        httpTesting.verify();
      }
    );

    it(
      'posts the stock decrease request to the product endpoint',
      () => {
        const productId =
          '22222222-2222-2222-2222-222222222222';

        const response:
          DecreaseStockResponse = {
            productId,
            remainingQuantity: 0,
            isDepleted: true,
            recommendationRequested:
              true,
            correlationId:
              'workflow-correlation'
          };

        let actual:
          DecreaseStockResponse | undefined;

        service
          .decreaseStock(
            productId,
            1
          )
          .subscribe(
            result => {
              actual = result;
            }
          );

        const request =
          httpTesting.expectOne(
            `/api/inventory/${productId}/decrease`
          );

        expect(
          request.request.method
        ).toBe('POST');

        expect(
          request.request.body
        ).toEqual({
          quantity: 1
        });

        request.flush(response);

        expect(actual).toEqual(response);
      }
    );

    it(
      'URL-encodes the product identifier',
      () => {
        service
          .decreaseStock(
            'product/with space',
            2
          )
          .subscribe();

        const request =
          httpTesting.expectOne(
            '/api/inventory/product%2Fwith%20space/decrease'
          );

        expect(
          request.request.method
        ).toBe('POST');

        request.flush({
          productId:
            'product/with space',
          remainingQuantity: 3,
          isDepleted: false,
          recommendationRequested:
            false,
          correlationId:
            'correlation'
        });
      }
    );
  }
);