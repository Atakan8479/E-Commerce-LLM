import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output
} from '@angular/core';

import {
  DatePipe
} from '@angular/common';

import {
  CatalogItem
} from '../../../core/models/catalog.models';

import {
  RecommendationSource
} from '../../../core/models/recommendation.models';

import {
  RecommendationPollingState
} from '../recommendation-polling.store';

@Component({
  selector: 'app-recommendation-panel',
  imports: [
    DatePipe
  ],
  templateUrl: './recommendation-panel.html',
  styleUrl: './recommendation-panel.css',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class RecommendationPanel {
  readonly state =
    input.required<RecommendationPollingState>();

  readonly catalogItems =
    input.required<readonly CatalogItem[]>();

  readonly retry =
    output<void>();

  private readonly productNames =
    computed(
      () =>
        new Map(
          this.catalogItems().map(
            item => [
              item.productId,
              item.name
            ] as const
          )
        )
    );

  productName(
    productId: string
  ): string {
    return (
      this.productNames().get(
        productId
      ) ??
      'Recommended product'
    );
  }

  sourceLabel(
    source: RecommendationSource
  ): string {
    return (
      source === 'Llm'
        ? 'LLM'
        : source
    );
  }

  retryPolling(): void {
    this.retry.emit();
  }
}