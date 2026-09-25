import {
  inject,
  Injectable
} from '@angular/core';

import {
  HttpClient,
  HttpHeaders,
  HttpParams
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  RecommendationPlansResponse
} from '../models/recommendation.models';

@Injectable({
  providedIn: 'root'
})
export class RecommendationApiService {
  private static readonly endpoint =
    '/api/recommendation-plans';

  private readonly http =
    inject(HttpClient);

  getByCorrelationId(
    correlationId: string
  ): Observable<RecommendationPlansResponse> {
    const params =
      new HttpParams()
        .set(
          'correlationId',
          correlationId
        );

    const headers =
      new HttpHeaders()
        .set(
          'X-Correlation-ID',
          correlationId
        );

    return this.http.get<RecommendationPlansResponse>(
      RecommendationApiService.endpoint,
      {
        params,
        headers
      }
    );
  }
}