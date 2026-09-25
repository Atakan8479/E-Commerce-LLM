import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  TestBed
} from '@angular/core/testing';

import {
  NEVER,
  Subject,
  of,
  throwError
} from 'rxjs';

import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi
} from 'vitest';

import {
  RecommendationApiService
} from '../../core/api/recommendation-api.service';

import {
  RecommendationPlansResponse
} from '../../core/models/recommendation.models';

import {
  RecommendationPollingStore
} from './recommendation-polling.store';

describe(
  'RecommendationPollingStore',
  () => {
    const productId =
      '22222222-2222-2222-2222-222222222222';

    const correlationId =
      'workflow-correlation';

    let store:
      RecommendationPollingStore;

    let getByCorrelationId:
      ReturnType<typeof vi.fn>;

    beforeEach(
      () => {
        vi.useFakeTimers();

        getByCorrelationId =
          vi.fn();

        TestBed.configureTestingModule({
          providers: [
            RecommendationPollingStore,
            {
              provide:
                RecommendationApiService,
              useValue: {
                getByCorrelationId
              }
            }
          ]
        });

        store =
          TestBed.inject(
            RecommendationPollingStore
          );
      }
    );

    afterEach(
      () => {
        vi.useRealTimers();
      }
    );

    it(
      'moves from polling to ready when a recommendation plan arrives',
      async () => {
        getByCorrelationId
          .mockReturnValueOnce(
            of(
              createEmptyResponse()
            )
          )
          .mockReturnValueOnce(
            of(
              createReadyResponse()
            )
          );

        store.start(
          productId,
          correlationId
        );

        expect(
          store.stateFor(
            productId
          ).status
        ).toBe('polling');

        await vi.advanceTimersByTimeAsync(
          0
        );

        expect(
          getByCorrelationId
        ).toHaveBeenCalledTimes(1);

        expect(
          store.stateFor(
            productId
          ).status
        ).toBe('polling');

        await vi.advanceTimersByTimeAsync(
          2_000
        );

        expect(
          getByCorrelationId
        ).toHaveBeenCalledTimes(2);

        const state =
          store.stateFor(
            productId
          );

        expect(
          state.status
        ).toBe('ready');

        expect(
          state.correlationId
        ).toBe(correlationId);

        expect(
          state.plans
        ).toHaveLength(1);

        expect(
          state.plans[0]
            ?.source
        ).toBe('Cache');

        expect(
          state.plans[0]
            ?.recommendations[0]
            ?.productId
        ).toBe(
          '11111111-1111-1111-1111-111111111111'
        );
      }
    );

    it(
      'times out after the maximum number of empty polling attempts',
      async () => {
        getByCorrelationId.mockReturnValue(
          of(
            createEmptyResponse()
          )
        );

        store.start(
          productId,
          correlationId
        );

        await vi.advanceTimersByTimeAsync(
          60_000
        );

        const state =
          store.stateFor(
            productId
          );

        expect(
          getByCorrelationId
        ).toHaveBeenCalledTimes(30);

        expect(
          state.status
        ).toBe('timeout');

        expect(
          state.message
        ).toContain(
          'still pending'
        );
      }
    );

    it(
      'moves to error when the recommendation API fails',
      async () => {
        getByCorrelationId.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 503,
                error: {
                  detail:
                    'Recommendation service unavailable.'
                }
              })
          )
        );

        store.start(
          productId,
          correlationId
        );

        await vi.advanceTimersByTimeAsync(
          0
        );

        const state =
          store.stateFor(
            productId
          );

        expect(
          state.status
        ).toBe('error');

        expect(
          state.message
        ).toBe(
          'Recommendation service unavailable.'
        );
      }
    );

    it(
      'does not overlap polling requests while a previous request is still active',
      async () => {
        const responseSubject =
          new Subject<
            RecommendationPlansResponse
          >();

        getByCorrelationId.mockReturnValue(
          responseSubject.asObservable()
        );

        store.start(
          productId,
          correlationId
        );

        await vi.advanceTimersByTimeAsync(
          0
        );

        expect(
          getByCorrelationId
        ).toHaveBeenCalledTimes(1);

        await vi.advanceTimersByTimeAsync(
          10_000
        );

        expect(
          getByCorrelationId
        ).toHaveBeenCalledTimes(1);

        responseSubject.next(
          createReadyResponse()
        );

        responseSubject.complete();

        expect(
          store.stateFor(
            productId
          ).status
        ).toBe('ready');
      }
    );

    it(
      'restarts polling with the existing correlation id when retry is requested',
      async () => {
        getByCorrelationId.mockReturnValue(
          throwError(
            () =>
              new HttpErrorResponse({
                status: 500,
                error: {
                  detail:
                    'Temporary failure.'
                }
              })
          )
        );

        store.start(
          productId,
          correlationId
        );

        await vi.advanceTimersByTimeAsync(
          0
        );

        expect(
          store.stateFor(
            productId
          ).status
        ).toBe('error');

        getByCorrelationId.mockReset();

        getByCorrelationId.mockReturnValue(
          of(
            createReadyResponse()
          )
        );

        store.retry(
          productId
        );

        await vi.advanceTimersByTimeAsync(
          0
        );

        expect(
          getByCorrelationId
        ).toHaveBeenCalledWith(
          correlationId
        );

        expect(
          store.stateFor(
            productId
          ).status
        ).toBe('ready');
      }
    );

    it(
      'ignores retry when no correlation id is available',
      () => {
        store.retry(
          productId
        );

        expect(
          getByCorrelationId
        ).not.toHaveBeenCalled();
      }
    );
  }
);

function createEmptyResponse():
  RecommendationPlansResponse {
  return {
    correlationId:
      'workflow-correlation',
    plans: []
  };
}

function createReadyResponse():
  RecommendationPlansResponse {
  return {
    correlationId:
      'workflow-correlation',
    plans: [
      {
        eventId:
          '16367fbb-a0ea-4f35-a2d0-a8925d1c4f95',

        originalProductId:
          '22222222-2222-2222-2222-222222222222',

        recommendations: [
          {
            productId:
              '11111111-1111-1111-1111-111111111111',

            reason:
              'Suitable in-stock alternative.'
          }
        ],

        source:
          'Cache',

        createdAtUtc:
          '2026-09-25T11:44:40.4265133Z'
      }
    ]
  };
}