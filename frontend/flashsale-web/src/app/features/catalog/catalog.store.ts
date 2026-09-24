import {
  computed,
  inject,
  Injectable,
  signal
} from '@angular/core';

import {
  HttpErrorResponse
} from '@angular/common/http';

import {
  take
} from 'rxjs';

import {
  CatalogApiService
} from '../../core/api/catalog-api.service';

import {
  CatalogItem
} from '../../core/models/catalog.models';

type CatalogLoadStatus =
  | 'idle'
  | 'loading'
  | 'loaded'
  | 'error';

interface CatalogState {
  readonly status: CatalogLoadStatus;
  readonly items: readonly CatalogItem[];
  readonly errorMessage: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class CatalogStore {
  private readonly catalogApi =
    inject(CatalogApiService);

  private readonly state =
    signal<CatalogState>({
      status: 'idle',
      items: [],
      errorMessage: null
    });

  readonly items =
    computed(
      () => this.state().items
    );

  readonly isLoading =
    computed(
      () => this.state().status === 'loading'
    );

  readonly hasError =
    computed(
      () => this.state().status === 'error'
    );

  readonly errorMessage =
    computed(
      () => this.state().errorMessage
    );

  readonly isEmpty =
    computed(
      () =>
        this.state().status === 'loaded' &&
        this.state().items.length === 0
    );

  readonly hasItems =
    computed(
      () => this.state().items.length > 0
    );

  ensureLoaded(): void {
    if (this.state().status !== 'idle') {
      return;
    }

    this.load();
  }

  reload(): void {
    this.load();
  }

  applyInventoryUpdate(
    productId: string,
    availableQuantity: number,
    isDepleted: boolean
  ): void {
    this.state.update(
      currentState => ({
        ...currentState,
        items:
          currentState.items.map(
            item =>
              item.productId === productId
                ? {
                    ...item,
                    availableQuantity,
                    isDepleted
                  }
                : item
          )
      })
    );
  }

  private load(): void {
    if (this.state().status === 'loading') {
      return;
    }

    this.state.update(
      currentState => ({
        ...currentState,
        status: 'loading',
        errorMessage: null
      })
    );

    this.catalogApi
      .getCatalog()
      .pipe(
        take(1)
      )
      .subscribe({
        next: response => {
          this.state.set({
            status: 'loaded',
            items: response.items,
            errorMessage: null
          });
        },
        error: (error: unknown) => {
          this.state.set({
            status: 'error',
            items: [],
            errorMessage:
              this.resolveErrorMessage(
                error
              )
          });
        }
      });
  }

  private resolveErrorMessage(
    error: unknown
  ): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'The catalog could not be loaded.';
    }

    if (error.status === 0) {
      return (
        'The API could not be reached. ' +
        'Verify that the backend is running.'
      );
    }

    const problemDetail =
      this.tryGetProblemDetail(
        error.error
      );

    if (problemDetail !== null) {
      return problemDetail;
    }

    return (
      `The catalog request failed ` +
      `with status ${error.status}.`
    );
  }

  private tryGetProblemDetail(
    payload: unknown
  ): string | null {
    if (
      typeof payload !== 'object' ||
      payload === null ||
      !('detail' in payload)
    ) {
      return null;
    }

    const detail =
      (payload as {
        detail?: unknown;
      }).detail;

    if (
      typeof detail !== 'string' ||
      detail.trim().length === 0
    ) {
      return null;
    }

    return detail;
  }
}