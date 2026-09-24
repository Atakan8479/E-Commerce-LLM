import {
  inject,
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  CatalogResponse
} from '../models/catalog.models';

@Injectable({
  providedIn: 'root'
})
export class CatalogApiService {
  private static readonly endpoint =
    '/api/catalog';

  private readonly http =
    inject(HttpClient);

  getCatalog(): Observable<CatalogResponse> {
    return this.http.get<CatalogResponse>(
      CatalogApiService.endpoint
    );
  }
}