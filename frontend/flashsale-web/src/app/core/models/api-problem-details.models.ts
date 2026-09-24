export interface ApiProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errorCode?: string;
  readonly correlationId?: string;

  readonly errors?: Readonly<
    Record<string, readonly string[]>
  >;
}