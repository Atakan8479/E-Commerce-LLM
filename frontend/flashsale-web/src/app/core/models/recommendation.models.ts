export type RecommendationSource =
  | 'Cache'
  | 'Llm'
  | 'Deterministic';

export interface RecommendationPlansResponse {
  readonly correlationId: string;
  readonly plans: readonly RecommendationPlan[];
}

export interface RecommendationPlan {
  readonly eventId: string;
  readonly originalProductId: string;
  readonly recommendations:
    readonly RecommendationPlanItem[];

  readonly source: RecommendationSource;
  readonly createdAtUtc: string;
}

export interface RecommendationPlanItem {
  readonly productId: string;
  readonly reason: string;
}