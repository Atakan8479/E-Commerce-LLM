import {
  Locator,
  Page
} from '@playwright/test';

export function productCard(
  page: Page,
  productName: string
): Locator {
  return page
    .locator(
      'article.product-card'
    )
    .filter({
      has:
        page.getByRole(
          'heading',
          {
            name:
              productName,
            exact:
              true
          }
        )
    });
}

export function productGridItem(
  page: Page,
  productName: string
): Locator {
  return page
    .locator(
      '.catalog-grid__item'
    )
    .filter({
      has:
        page.getByRole(
          'heading',
          {
            name:
              productName,
            exact:
              true
          }
        )
    });
}