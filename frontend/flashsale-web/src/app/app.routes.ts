import {
  Routes
} from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'catalog'
  },
  {
    path: 'catalog',
    loadComponent: () =>
      import(
        './features/catalog/catalog-page/catalog-page'
      ).then(
        module =>
          module.CatalogPage
      )
  },
  {
    path: '**',
    redirectTo: 'catalog'
  }
];