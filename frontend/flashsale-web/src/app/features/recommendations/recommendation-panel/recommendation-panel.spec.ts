import {
  ComponentFixture,
  TestBed
} from '@angular/core/testing';

import {
  beforeEach,
  describe,
  expect,
  it,
  vi
} from 'vitest';

import {
  CatalogItem
} from '../../../core/models/catalog.models';

import {
  RecommendationPollingState
} from '../recommendation-polling.store';

import {
  RecommendationPanel
} from './recommendation-panel';

describe(
  'RecommendationPanel',
  () => {
    let fixture:
      ComponentFixture<RecommendationPanel>;

    let component:
      RecommendationPanel;

    beforeEach(
      async () => {
        await TestBed
          .configureTestingModule({
            imports: [
              RecommendationPanel
            ]
          })
          .compileComponents();

        fixture =
          TestBed.createComponent(
            RecommendationPanel
          );

        component =
          fixture.componentInstance;

        fixture.componentRef.setInput(
          'catalogItems',
          createCatalogItems()
        );

        fixture.componentRef.setInput(
          'state',
          createState()
        );

        fixture.detectChanges();
      }
    );

    it(
      'renders the polling state',
      () => {
        fixture.componentRef.setInput(
          'state',
          createState({
            status: 'polling',
            message:
              'Waiting for recommendation generation.'
          })
        );

        fixture.detectChanges();

        expect(
          fixture.nativeElement.textContent
        ).toContain(
          'Processing recommendations'
        );

        expect(
          fixture.nativeElement.textContent
        ).toContain(
          'Waiting for recommendation generation.'
        );
      }
    );

    it(
      'renders a ready recommendation plan',
      () => {
        fixture.componentRef.setInput(
          'state',
          createReadyState()
        );

        fixture.detectChanges();

        const text =
          fixture.nativeElement.textContent;

        expect(text).toContain('Cache');

        expect(text).toContain(
          'Wireless Gaming Mouse'
        );

        expect(text).toContain(
          'Suitable in-stock alternative.'
        );

        expect(text).toContain(
          'event-1'
        );
      }
    );

    it(
      'renders Llm source as LLM',
      () => {
        const ready =
          createReadyState();

        fixture.componentRef.setInput(
          'state',
          {
            ...ready,
            plans: [
              {
                ...ready.plans[0]!,
                source: 'Llm'
              }
            ]
          }
        );

        fixture.detectChanges();

        expect(
          fixture.nativeElement.textContent
        ).toContain('LLM');
      }
    );

    it(
      'renders the empty alternative state',
      () => {
        const ready =
          createReadyState();

        fixture.componentRef.setInput(
          'state',
          {
            ...ready,
            plans: [
              {
                ...ready.plans[0]!,
                recommendations: []
              }
            ]
          }
        );

        fixture.detectChanges();

        expect(
          fixture.nativeElement.textContent
        ).toContain(
          'No in-stock alternatives were returned.'
        );
      }
    );

    it(
      'falls back when the recommended product is absent from the catalog',
      () => {
        const ready =
          createReadyState();

        fixture.componentRef.setInput(
          'catalogItems',
          []
        );

        fixture.componentRef.setInput(
          'state',
          ready
        );

        fixture.detectChanges();

        expect(
          fixture.nativeElement.textContent
        ).toContain(
          'Recommended product'
        );
      }
    );

    it(
      'renders timeout state and emits retry',
      () => {
        const retry =
          vi.fn();

        component.retry.subscribe(
          retry
        );

        fixture.componentRef.setInput(
          'state',
          createState({
            status: 'timeout',
            message:
              'Recommendation is still pending.'
          })
        );

        fixture.detectChanges();

        const button =
          fixture.nativeElement.querySelector(
            '.recommendation-panel__retry'
          ) as HTMLButtonElement;

        button.click();

        expect(
          fixture.nativeElement.textContent
        ).toContain(
          'Recommendation still pending'
        );

        expect(retry).toHaveBeenCalledTimes(1);
      }
    );

    it(
      'renders error state and correlation id',
      () => {
        fixture.componentRef.setInput(
          'state',
          createState({
            status: 'error',
            message:
              'Recommendation service unavailable.',
            correlationId:
              'workflow-correlation'
          })
        );

        fixture.detectChanges();

        const text =
          fixture.nativeElement.textContent;

        expect(text).toContain(
          'Recommendation unavailable'
        );

        expect(text).toContain(
          'Recommendation service unavailable.'
        );

        expect(text).toContain(
          'workflow-correlation'
        );
      }
    );
  }
);

function createCatalogItems():
  readonly CatalogItem[] {
  return [
    {
      productId:
        '11111111-1111-1111-1111-111111111111',
      name:
        'Wireless Gaming Mouse',
      category:
        'gaming-mouse',
      availableQuantity: 5,
      isDepleted: false
    }
  ];
}

function createState(
  overrides:
    Partial<RecommendationPollingState> = {}
): RecommendationPollingState {
  return {
    status: 'idle',
    correlationId: null,
    plans: [],
    message: null,
    ...overrides
  };
}

function createReadyState():
  RecommendationPollingState {
  return {
    status: 'ready',
    correlationId:
      'workflow-correlation',
    message: null,
    plans: [
      {
        eventId:
          'event-1',
        originalProductId:
          '22222222-2222-2222-2222-222222222222',
        source:
          'Cache',
        createdAtUtc:
          '2026-09-25T11:44:40.4265133Z',
        recommendations: [
          {
            productId:
              '11111111-1111-1111-1111-111111111111',
            reason:
              'Suitable in-stock alternative.'
          }
        ]
      }
    ]
  };
}