import {
  expect,
  test
} from '@playwright/test';

import {
  productCard,
  productGridItem
} from './support/catalog.po';

import {
  resetFlashSaleFixture
} from './support/database-fixture';

test.describe(
  'Flash-sale full-stack workflow',
  () => {
    test.beforeEach(
      async ({
        page
      }) => {
        resetFlashSaleFixture();

        await page.goto(
          '/catalog'
        );

        await expect(
          page.getByRole(
            'heading',
            {
              name:
                'Product Catalog'
            }
          )
        ).toBeVisible();
      }
    );

    test(
      'loads the catalog from the real backend',
      async ({
        page
      }) => {
        const alternative =
          productCard(
            page,
            'Wireless Gaming Mouse'
          );

        const trigger =
          productCard(
            page,
            'Wireless Gaming Mouse Pro'
          );

        const depleted =
          productCard(
            page,
            'RGB Gaming Mouse'
          );

        await expect(
          alternative
        ).toContainText(
          '5 in stock'
        );

        await expect(
          trigger
        ).toContainText(
          '1 in stock'
        );

        await expect(
          depleted
        ).toContainText(
          'Out of stock'
        );
      }
    );

    test(
      'decreases inventory through the real API',
      async ({
        page
      }) => {
        const card =
          productCard(
            page,
            'Wireless Gaming Mouse'
          );

        const quantity =
          card.locator(
            'input[type="number"]'
          );

        await quantity.fill(
          '2'
        );

        await card
          .getByRole(
            'button',
            {
              name:
                'Decrease stock'
            }
          )
          .click();

        await expect(
          card
        ).toContainText(
          '3 in stock'
        );

        await expect(
          card
        ).toContainText(
          'Stock updated successfully. 3 items remaining.'
        );

        await expect(
          card
        ).toContainText(
          'Correlation:'
        );
      }
    );

    test(
      'renders an asynchronous recommendation after stock depletion',
      async ({
        page
      }) => {
        const recommendationRequests:
          Array<{
            correlationId:
              string | null;

            headerCorrelationId:
              string | undefined;
          }> = [];

        page.on(
          'request',
          request => {
            const url =
              new URL(
                request.url()
              );

            if (
              url.pathname !==
              '/api/recommendation-plans'
            ) {
              return;
            }

            recommendationRequests.push({
              correlationId:
                url.searchParams.get(
                  'correlationId'
                ),

              headerCorrelationId:
                request.headers()[
                  'x-correlation-id'
                ]
            });
          }
        );

        const card =
          productCard(
            page,
            'Wireless Gaming Mouse Pro'
          );

        const gridItem =
          productGridItem(
            page,
            'Wireless Gaming Mouse Pro'
          );

        await card
          .getByRole(
            'button',
            {
              name:
                'Decrease stock'
            }
          )
          .click();

        await expect(
          card
        ).toContainText(
          'Stock depleted.'
        );

        const feedbackText =
          await card.textContent();

        const correlationMatch =
          feedbackText?.match(
            /Correlation:\s*([a-f0-9]+)/i
          );

        expect(
          correlationMatch
        ).not.toBeNull();

        const correlationId =
          correlationMatch![1];

        const panel =
          gridItem.locator(
            'app-recommendation-panel'
          );

        await expect(
          panel
        ).toBeVisible({
          timeout:
            60_000
        });

        await expect(
          panel
        ).toContainText(
          'Wireless Gaming Mouse',
          {
            timeout:
              60_000
          }
        );

        const recommendationSource =
          panel.locator(
            '.recommendation-plan__source'
          );

        const expectedRecommendationSource =
          process.env[
            'E2E_EXPECTED_RECOMMENDATION_SOURCE'
          ];

        if (
          expectedRecommendationSource !==
          undefined
        ) {
          await expect(
            recommendationSource
          ).toContainText(
            expectedRecommendationSource
          );
        } else {
          await expect(
            recommendationSource
          ).toHaveText(
            /^\s*(LLM|Cache|Deterministic)\s*$/
          );
        }

        await expect(
          panel
        ).toContainText(
          `Correlation: ${correlationId}`
        );

        expect(
          recommendationRequests.length
        ).toBeGreaterThan(0);

        for (
          const request of
          recommendationRequests
        ) {
          expect(
            request.correlationId
          ).toBe(
            correlationId
          );

          expect(
            request.headerCorrelationId
          ).toBe(
            correlationId
          );
        }
      }
    );
  }
);