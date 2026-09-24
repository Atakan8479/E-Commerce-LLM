export interface CatalogResponse {
  readonly items: readonly CatalogItem[];
}

export interface CatalogItem {
  readonly productId: string;
  readonly name: string;
  readonly category: string;
  readonly availableQuantity: number;
  readonly isDepleted: boolean;
}