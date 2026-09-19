import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type CustomerTier = 'RETAIL' | 'VIP' | 'WHOLESALE';
export type MeasureType = 'Weight' | 'Count';
export type ActionType = 'OverridePrice' | 'PercentageDiscount' | 'AmountOff';
export type CalculationBase = 'OriginalBase' | 'RunningTotal';

export interface Product {
  id: number;
  name: string;
  variants: ProductVariant[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ProductVariant {
  id: number;
  sku: string;
  uomCode: string;
  uomFactor: number;
  basePrice: number;
  isActive?: boolean;
  unitOfMeasure: UnitOfMeasure;
}

export interface UnitOfMeasure {
  code: string;
  measureType: MeasureType;
}

export interface PriceRuleCondition {
  attribute: string;
  operator: string;
  value: string;
}

export interface PriceRuleAction {
  actionType: ActionType;
  amount: number;
  calculationBase: CalculationBase;
}

export interface AdminCatalog {
  products: PagedResult<AdminProduct>;
  unitsOfMeasure: UnitOfMeasure[];
  priceRules: PagedResult<AdminPriceRule>;
}

export interface AdminProduct {
  id: number;
  name: string;
  variants: AdminProductVariant[];
}

export interface AdminProductVariant {
  id: number;
  sku: string;
  uomCode: string;
  uomFactor: number;
  basePrice: number;
  isActive: boolean;
  measureType: MeasureType;
}

export interface AdminPriceRule {
  id: number;
  name: string;
  priority: number;
  isStackable: boolean;
  appliesToAllVariants: boolean;
  status: 'Active' | 'Inactive';
  targetVariantIds: number[];
  targetCustomerTiers: string[];
  conditions: PriceRuleCondition[];
  actions: PriceRuleAction[];
}

export interface CreatePriceRuleRequest {
  name: string;
  priority: number;
  isStackable: boolean;
  appliesToAllVariants: boolean;
  variantIds: number[];
  customerTiers: string[];
  conditions: PriceRuleCondition[];
  actions: PriceRuleAction[];
}

export interface ProductVariantPriceRulesResponse {
  variant: AdminProductVariant;
  assignedPriceRules: AdminPriceRule[];
  availablePriceRules: AdminPriceRule[];
  globalPriceRules: AdminPriceRule[];
}

export interface RelatedPriceRule {
  id: number;
  name: string;
  priority: number;
  isStackable: boolean;
  conditions: PriceRuleCondition[];
  actions: PriceRuleAction[];
}

export interface OrderItemPreview {
  variantId: number;
  sku: string;
  unitOfMeasure: string;
  baseUnitPrice: number;
  quantity: number;
  unitPriceApplied: number;
  lineSubtotal: number;
  priceChangeReason: string;
  activePriceRules: RelatedPriceRule[];
}

export interface OrderResponse {
  id: number;
  customerTier: CustomerTier;
  createdAt: string;
  items: OrderItemResponse[];
}

export interface OrderListItem {
  id: number;
  createdAt: string;
}

export interface OrderItemResponse {
  id: number;
  variantId: number;
  sku: string;
  unitOfMeasure: string;
  quantity: number;
  unitPriceApplied: number;
  totalLineAmount: number;
  priceChangeReason: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  getProducts(page = 1, pageSize = 25): Observable<PagedResult<Product>> {
    return this.http.get<PagedResult<Product>>('/api/products', { params: { page, pageSize } });
  }

  getAdminCatalog(productPage = 1, priceRulePage = 1, pageSize = 25): Observable<AdminCatalog> {
    return this.http.get<AdminCatalog>('/api/admin/catalog', {
      params: {
        productPage,
        productPageSize: pageSize,
        priceRulePage,
        priceRulePageSize: pageSize
      }
    });
  }

  createAdminProduct(name: string): Observable<AdminProduct> {
    return this.http.post<AdminProduct>('/api/admin/products', { name });
  }

  createProductVariant(productId: number, request: {
    sku: string;
    uomCode: string;
    uomFactor: number;
    basePrice: number;
  }): Observable<AdminProductVariant> {
    return this.http.post<AdminProductVariant>(`/api/admin/products/${productId}/variants`, request);
  }

  setProductVariantStatus(variantId: number, isActive: boolean): Observable<AdminProductVariant> {
    return this.http.patch<AdminProductVariant>(`/api/admin/variants/${variantId}/status`, { isActive });
  }

  createPriceRule(request: CreatePriceRuleRequest): Observable<AdminPriceRule> {
    return this.http.post<AdminPriceRule>('/api/admin/price-rules', request);
  }

  getProductVariantPriceRules(variantId: number): Observable<ProductVariantPriceRulesResponse> {
    return this.http.get<ProductVariantPriceRulesResponse>(`/api/admin/variants/${variantId}/price-rules`);
  }

  assignPriceRuleToProductVariant(variantId: number, priceRuleId: number): Observable<AdminPriceRule> {
    return this.http.put<AdminPriceRule>(`/api/admin/variants/${variantId}/price-rules/${priceRuleId}`, null);
  }

  removePriceRuleFromProductVariant(variantId: number, priceRuleId: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/variants/${variantId}/price-rules/${priceRuleId}`);
  }

  getOrders(page = 1, pageSize = 25): Observable<PagedResult<OrderListItem>> {
    return this.http.get<PagedResult<OrderListItem>>('/api/orders', { params: { page, pageSize } });
  }

  getOrder(orderId: number): Observable<OrderResponse> {
    return this.http.get<OrderResponse>(`/api/orders/${orderId}`);
  }

  calculateOrderItem(request: {
    customerTier: CustomerTier;
    variantId: number;
    quantity: number;
    cartSubtotal: number;
  }): Observable<OrderItemPreview> {
    return this.http.post<OrderItemPreview>('/api/orders/calculate-item', request);
  }

  submitOrder(customerTier: CustomerTier, items: Array<{ variantId: number; quantity: number }>): Observable<OrderResponse> {
    return this.http.post<OrderResponse>('/api/orders/submit', { customerTier, items });
  }
}