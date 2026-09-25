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
  CatalogResponse
} from '../models/catalog.models';

import {
  CatalogApiService
} from './catalog-api.service';

describe(
  'CatalogApiService',
  () => {
    let service:
      CatalogApiService;

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
            CatalogApiService
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
      'requests the catalog from the expected endpoint',
      () => {
        const response:
          CatalogResponse = {
            items: [
              {
                productId:
                  'product-1',
                name:
                  'Wireless Gaming Mouse',
                category:
                  'gaming-mouse',
                availableQuantity: 5,
                isDepleted: false
              }
            ]
          };

        let actual:
          CatalogResponse | undefined;

        service
          .getCatalog()
          .subscribe(
            result => {
              actual = result;
            }
          );

        const request =
          httpTesting.expectOne(
            '/api/catalog'
          );

        expect(
          request.request.method
        ).toBe('GET');

        request.flush(response);

        expect(actual).toEqual(response);
      }
    );
  }
);