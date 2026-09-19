import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideChevronLeft, LucideChevronRight, LucideCirclePlus, LucideTrash2, LucideX } from '@lucide/angular';
import { finalize, switchMap } from 'rxjs';
import { TopLinksBarComponent } from '../shared/top-links-bar.component';
import { AdminPriceRule, ApiService, PriceRuleAction, Product, ProductVariantPriceRulesResponse } from '../services/api.service';

@Component({
  selector: 'app-product-variant-price-rules',
  imports: [
    CurrencyPipe,
    LucideChevronLeft,
    LucideChevronRight,
    LucideCirclePlus,
    LucideTrash2,
    LucideX,
    TopLinksBarComponent
  ],
  templateUrl: './product-variant-price-rules.component.html',
  styleUrl: './product-variant-price-rules.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductVariantPriceRulesComponent {
  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly products = signal<Product[]>([]);
  protected readonly productPage = signal(1);
  protected readonly productTotalPages = signal(0);
  protected readonly productsLoading = signal(true);
  protected readonly selectedVariantId = signal<number | null>(null);
  protected readonly priceRules = signal<ProductVariantPriceRulesResponse | null>(null);
  protected readonly priceRulesLoading = signal(false);
  protected readonly changingRuleIds = signal<ReadonlySet<number>>(new Set());
  protected readonly error = signal<string | null>(null);
  protected readonly confirmation = signal<string | null>(null);

  constructor() {
    this.loadProducts();
  }

  protected selectVariant(variantId: number): void {
    if (this.selectedVariantId() === variantId && this.priceRules()) {
      return;
    }

    this.beginRequest();
    this.selectedVariantId.set(variantId);
    this.loadPriceRules(variantId);
  }

  protected changeProductPage(page: number): void {
    if (page < 1 || page > this.productTotalPages() || page === this.productPage()) {
      return;
    }

    this.productPage.set(page);
    this.loadProducts();
  }

  protected assignPriceRule(priceRuleId: number): void {
    const variantId = this.selectedVariantId();
    if (variantId === null || this.changingRuleIds().has(priceRuleId)) {
      return;
    }

    this.beginRequest();
    this.markRuleChanging(priceRuleId, true);
    this.apiService.assignPriceRuleToProductVariant(variantId, priceRuleId)
      .pipe(
        switchMap(() => this.apiService.getProductVariantPriceRules(variantId)),
        finalize(() => this.markRuleChanging(priceRuleId, false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (priceRules) => {
          this.setPriceRules(priceRules);
          this.confirmation.set('Price rule assigned to this product variant.');
        },
        error: () => this.error.set('The price rule could not be assigned. Refresh the page and try again.')
      });
  }

  protected removePriceRule(priceRuleId: number): void {
    const variantId = this.selectedVariantId();
    if (variantId === null || this.changingRuleIds().has(priceRuleId)) {
      return;
    }

    this.beginRequest();
    this.markRuleChanging(priceRuleId, true);
    this.apiService.removePriceRuleFromProductVariant(variantId, priceRuleId)
      .pipe(
        switchMap(() => this.apiService.getProductVariantPriceRules(variantId)),
        finalize(() => this.markRuleChanging(priceRuleId, false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (priceRules) => {
          this.setPriceRules(priceRules);
          this.confirmation.set('Price rule removed from this product variant.');
        },
        error: () => this.error.set('The price rule could not be removed. Refresh the page and try again.')
      });
  }

  protected describeAction(action: PriceRuleAction): string {
    switch (action.actionType) {
      case 'OverridePrice':
        return `Set to $${action.amount.toFixed(2)}`;
      case 'PercentageDiscount':
        return `${action.amount}% off`;
      case 'AmountOff':
        return `$${action.amount.toFixed(2)} off`;
    }
  }

  protected conditionSummary(rule: AdminPriceRule): string {
    return rule.conditions.length === 0
      ? 'No conditions'
      : `${rule.conditions.length} condition${rule.conditions.length === 1 ? '' : 's'}`;
  }

  private loadProducts(): void {
    this.productsLoading.set(true);
    this.apiService.getProducts(this.productPage())
      .pipe(
        finalize(() => this.productsLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (products) => {
          this.products.set(products.items);
          this.productPage.set(products.page);
          this.productTotalPages.set(products.totalPages);
        },
        error: () => this.error.set('Product variants could not be loaded. Refresh the page to try again.')
      });
  }

  private loadPriceRules(variantId: number): void {
    this.priceRulesLoading.set(true);
    this.apiService.getProductVariantPriceRules(variantId)
      .pipe(
        finalize(() => this.priceRulesLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (priceRules) => this.setPriceRules(priceRules),
        error: () => {
          this.priceRules.set(null);
          this.error.set('Price rules for this product variant could not be loaded. Select another variant to try again.');
        }
      });
  }

  private setPriceRules(priceRules: ProductVariantPriceRulesResponse): void {
    this.priceRules.set({
      ...priceRules,
      assignedPriceRules: priceRules.assignedPriceRules ?? [],
      availablePriceRules: priceRules.availablePriceRules ?? [],
      globalPriceRules: priceRules.globalPriceRules ?? []
    });
  }

  private markRuleChanging(priceRuleId: number, isChanging: boolean): void {
    this.changingRuleIds.update((ruleIds) => {
      const nextIds = new Set(ruleIds);
      isChanging ? nextIds.add(priceRuleId) : nextIds.delete(priceRuleId);
      return nextIds;
    });
  }

  private beginRequest(): void {
    this.error.set(null);
    this.confirmation.set(null);
  }
}