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
  RecommendationPlansResponse
} from '../models/recommendation.models';

import {
  RecommendationApiService
} from './recommendation-api.service';

describe(
  'RecommendationApiService',
  () => {
    let service:
      RecommendationApiService;

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
            RecommendationApiService
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
      'sends the workflow correlation id in both query string and header',
      () => {
        const correlationId =
          '9d3a8f5f340b49f58c9b31a891adbe3d';

        const response:
          RecommendationPlansResponse = {
            correlationId,
            plans: []
          };

        let actual:
          RecommendationPlansResponse | undefined;

        service
          .getByCorrelationId(
            correlationId
          )
          .subscribe(
            result => {
              actual = result;
            }
          );

        const request =
          httpTesting.expectOne(
            req =>
              req.url ===
                '/api/recommendation-plans' &&
              req.params.get(
                'correlationId'
              ) === correlationId
          );

        expect(
          request.request.method
        ).toBe('GET');

        expect(
          request.request.headers.get(
            'X-Correlation-ID'
          )
        ).toBe(correlationId);

        request.flush(response);

        expect(actual).toEqual(response);
      }
    );
  }
);