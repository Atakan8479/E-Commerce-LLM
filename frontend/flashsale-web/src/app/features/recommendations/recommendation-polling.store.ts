import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  inject,
  Injectable,
  signal
} from '@angular/core';

import {
  exhaustMap,
  Subscription,
  take,
  takeWhile,
  timer
} from 'rxjs';

import {
  RecommendationApiService
} from '../../core/api/recommendation-api.service';

import {
  ApiProblemDetails
} from '../../core/models/api-problem-details.models';

import {
  RecommendationPlan
} from '../../core/models/recommendation.models';

export type RecommendationPollingStatus =
  | 'idle'
  | 'polling'
  | 'ready'
  | 'timeout'
  | 'error';

export interface RecommendationPollingState {
  readonly status: RecommendationPollingStatus;
  readonly correlationId: string | null;
  readonly plans: readonly RecommendationPlan[];
  readonly message: string | null;
}

const idleState: RecommendationPollingState = {
  status: 'idle',
  correlationId: null,
  plans: [],
  message: null
};

@Injectable({
  providedIn: 'root'
})
export class RecommendationPollingStore {
  private static readonly pollingIntervalMs =
    2_000;

  private static readonly maximumAttempts =
    30;

  private readonly recommendationApi =
    inject(RecommendationApiService);

  private readonly states =
    signal<
      Readonly<
        Record<
          string,
          RecommendationPollingState
        >
      >
    >({});

  private readonly subscriptions =
    new Map<string, Subscription>();

  stateFor(
    productId: string
  ): RecommendationPollingState {
    return (
      this.states()[productId] ??
      idleState
    );
  }

  hasActivity(
    productId: string
  ): boolean {
    return (
      this.stateFor(productId).status !==
      'idle'
    );
  }

  start(
    productId: string,
    correlationId: string
  ): void {
    const normalizedCorrelationId =
      correlationId.trim();

    if (
      productId.trim().length === 0 ||
      normalizedCorrelationId.length === 0
    ) {
      return;
    }

    this.subscriptions
      .get(productId)
      ?.unsubscribe();

    this.setState(
      productId,
      {
        status: 'polling',
        correlationId:
          normalizedCorrelationId,
        plans: [],
        message:
          'Waiting for the recommendation workflow to complete.'
      }
    );

    let recommendationReceived =
      false;

    const subscription =
      timer(
        0,
        RecommendationPollingStore
          .pollingIntervalMs
      )
        .pipe(
          exhaustMap(
            () =>
              this.recommendationApi
                .getByCorrelationId(
                  normalizedCorrelationId
                )
          ),
          take(
            RecommendationPollingStore
              .maximumAttempts
          ),
          takeWhile(
            response =>
              response.plans.length === 0,
            true
          )
        )
        .subscribe({
          next: response => {
            if (
              response.plans.length === 0
            ) {
              return;
            }

            recommendationReceived =
              true;

            this.setState(
              productId,
              {
                status: 'ready',
                correlationId:
                  response.correlationId,
                plans:
                  response.plans,
                message: null
              }
            );
          },

          error: (error: unknown) => {
            this.setState(
              productId,
              {
                status: 'error',
                correlationId:
                  normalizedCorrelationId,
                plans: [],
                message:
                  this.resolveFailureMessage(
                    error
                  )
              }
            );

            this.subscriptions.delete(
              productId
            );
          },

          complete: () => {
            if (
              !recommendationReceived &&
              this.stateFor(productId)
                .status === 'polling'
            ) {
              this.setState(
                productId,
                {
                  status: 'timeout',
                  correlationId:
                    normalizedCorrelationId,
                  plans: [],
                  message:
                    'The recommendation workflow is still pending. ' +
                    'You can retry polling without starting another ' +
                    'inventory operation.'
                }
              );
            }

            this.subscriptions.delete(
              productId
            );
          }
        });

    this.subscriptions.set(
      productId,
      subscription
    );
  }

  retry(
    productId: string
  ): void {
    const state =
      this.stateFor(productId);

    if (
      state.correlationId === null ||
      state.status === 'polling'
    ) {
      return;
    }

    this.start(
      productId,
      state.correlationId
    );
  }

  private setState(
    productId: string,
    state: RecommendationPollingState
  ): void {
    this.states.update(
      currentStates => ({
        ...currentStates,
        [productId]: state
      })
    );
  }

  private resolveFailureMessage(
    error: unknown
  ): string {
    if (!(error instanceof HttpErrorResponse)) {
      return (
        'Recommendation polling could not be completed.'
      );
    }

    if (error.status === 0) {
      return (
        'The API could not be reached while ' +
        'waiting for recommendations.'
      );
    }

    const problemDetails =
      this.tryGetProblemDetails(
        error.error
      );

    if (
      typeof problemDetails?.detail ===
        'string' &&
      problemDetails.detail.trim().length > 0
    ) {
      return problemDetails.detail;
    }

    if (
      typeof problemDetails?.title ===
        'string' &&
      problemDetails.title.trim().length > 0
    ) {
      return problemDetails.title;
    }

    return (
      `Recommendation polling failed ` +
      `with status ${error.status}.`
    );
  }

  private tryGetProblemDetails(
    payload: unknown
  ): ApiProblemDetails | null {
    if (
      typeof payload !== 'object' ||
      payload === null
    ) {
      return null;
    }

    return payload as ApiProblemDetails;
  }
}