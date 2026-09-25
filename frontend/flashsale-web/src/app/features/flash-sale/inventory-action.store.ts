import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  inject,
  Injectable,
  signal
} from '@angular/core';

import {
  take
} from 'rxjs';

import {
  InventoryApiService
} from '../../core/api/inventory-api.service';

import {
  ApiProblemDetails
} from '../../core/models/api-problem-details.models';

import {
  CatalogStore
} from '../catalog/catalog.store';

import {
  RecommendationPollingStore
} from '../recommendations/recommendation-polling.store';

import {
  InventoryActionFeedback
} from './inventory-action.models';

type InventoryActionStatus =
  | 'submitting'
  | 'success'
  | 'error';

interface InventoryActionEntry {
  readonly status: InventoryActionStatus;
  readonly feedback: InventoryActionFeedback | null;
}

@Injectable({
  providedIn: 'root'
})
export class InventoryActionStore {
  private readonly inventoryApi =
    inject(InventoryApiService);

  private readonly catalogStore =
    inject(CatalogStore);

  private readonly recommendationPolling =
    inject(RecommendationPollingStore);

  private readonly actions =
    signal<
      Readonly<
        Record<string, InventoryActionEntry>
      >
    >({});

  isSubmitting(
    productId: string
  ): boolean {
    return (
      this.actions()[productId]?.status ===
      'submitting'
    );
  }

  feedbackFor(
    productId: string
  ): InventoryActionFeedback | null {
    return (
      this.actions()[productId]?.feedback ??
      null
    );
  }

  decreaseStock(
    productId: string,
    quantity: number
  ): void {
    if (
      this.isSubmitting(productId) ||
      !Number.isInteger(quantity) ||
      quantity < 1
    ) {
      return;
    }

    this.setAction(
      productId,
      {
        status: 'submitting',
        feedback: null
      }
    );

    this.inventoryApi
      .decreaseStock(
        productId,
        quantity
      )
      .pipe(
        take(1)
      )
      .subscribe({
        next: response => {
          this.catalogStore.applyInventoryUpdate(
            response.productId,
            response.remainingQuantity,
            response.isDepleted
          );

          this.setAction(
            productId,
            {
              status: 'success',
              feedback: {
                kind: 'success',
                message:
                  this.resolveSuccessMessage(
                    response.remainingQuantity,
                    response.recommendationRequested
                  ),
                errorCode: null,
                correlationId:
                  response.correlationId,
                result: response
              }
            }
          );

          if (
            response.recommendationRequested
          ) {
            this.recommendationPolling.start(
              response.productId,
              response.correlationId
            );
          }

          this.catalogStore.reload();
        },

        error: (error: unknown) => {
          this.setAction(
            productId,
            {
              status: 'error',
              feedback:
                this.resolveFailure(
                  error
                )
            }
          );

          if (
            error instanceof HttpErrorResponse &&
            (
              error.status === 404 ||
              error.status === 409
            )
          ) {
            this.catalogStore.reload();
          }
        }
      });
  }

  private setAction(
    productId: string,
    entry: InventoryActionEntry
  ): void {
    this.actions.update(
      currentActions => ({
        ...currentActions,
        [productId]: entry
      })
    );
  }

  private resolveSuccessMessage(
    remainingQuantity: number,
    recommendationRequested: boolean
  ): string {
    if (recommendationRequested) {
      return (
        'Stock depleted. ' +
        'The recommendation workflow was requested.'
      );
    }

    return (
      'Stock updated successfully. ' +
      `${remainingQuantity} item` +
      `${remainingQuantity === 1 ? '' : 's'} remaining.`
    );
  }

  private resolveFailure(
    error: unknown
  ): InventoryActionFeedback {
    if (!(error instanceof HttpErrorResponse)) {
      return {
        kind: 'error',
        message:
          'The inventory update could not be completed.',
        errorCode: null,
        correlationId: null,
        result: null
      };
    }

    if (error.status === 0) {
      return {
        kind: 'error',
        message:
          'The API could not be reached. ' +
          'Verify that the backend is running.',
        errorCode: null,
        correlationId: null,
        result: null
      };
    }

    const problemDetails =
      this.tryGetProblemDetails(
        error.error
      );

    return {
      kind: 'error',
      message:
        this.resolveProblemMessage(
          problemDetails,
          error.status
        ),
      errorCode:
        problemDetails?.errorCode ??
        null,
      correlationId:
        problemDetails?.correlationId ??
        null,
      result: null
    };
  }

  private resolveProblemMessage(
    problemDetails: ApiProblemDetails | null,
    status: number
  ): string {
    const validationMessage =
      this.tryGetValidationMessage(
        problemDetails
      );

    if (validationMessage !== null) {
      return validationMessage;
    }

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
      `The inventory request failed ` +
      `with status ${status}.`
    );
  }

  private tryGetValidationMessage(
    problemDetails: ApiProblemDetails | null
  ): string | null {
    if (problemDetails?.errors === undefined) {
      return null;
    }

    for (
      const messages of
      Object.values(problemDetails.errors)
    ) {
      const message =
        messages.find(
          value =>
            value.trim().length > 0
        );

      if (message !== undefined) {
        return message;
      }
    }

    return null;
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