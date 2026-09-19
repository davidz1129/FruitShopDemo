import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideChevronLeft, LucideChevronRight, LucideCirclePlus, LucidePower, LucidePowerOff, LucideSave, LucideX } from '@lucide/angular';
import { finalize } from 'rxjs';
import { TopLinksBarComponent } from '../shared/top-links-bar.component';
import {
  ActionType,
  AdminCatalog,
  AdminPriceRule,
  AdminProduct,
  AdminProductVariant,
  ApiService,
  CalculationBase,
  CreatePriceRuleRequest,
  CustomerTier,
  PagedResult,
  PriceRuleAction,
  PriceRuleCondition
} from '../services/api.service';

type RuleAttribute = 'Quantity' | 'CustomerTier' | 'CartSubtotal' | 'OrderDate';

@Component({
  selector: 'app-admin',
  imports: [
    CurrencyPipe,
    DecimalPipe,
    LucideChevronLeft,
    LucideChevronRight,
    LucideCirclePlus,
    LucidePower,
    LucidePowerOff,
    LucideSave,
    LucideX,
    ReactiveFormsModule,
    TopLinksBarComponent
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminComponent {
  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly customerTiers: CustomerTier[] = ['RETAIL', 'VIP', 'WHOLESALE'];
  protected readonly catalog = signal<AdminCatalog | null>(null);
  protected readonly productPage = signal(1);
  protected readonly priceRulePage = signal(1);
  protected readonly catalogLoading = signal(true);
  protected readonly productSaving = signal(false);
  protected readonly variantSaving = signal(false);
  protected readonly ruleSaving = signal(false);
  protected readonly changingVariantIds = signal<ReadonlySet<number>>(new Set());
  protected readonly error = signal<string | null>(null);
  protected readonly confirmation = signal<string | null>(null);
  protected readonly selectedVariantIds = signal<ReadonlySet<number>>(new Set());
  protected readonly selectedCustomerTiers = signal<ReadonlySet<CustomerTier>>(new Set());
  protected readonly activeVariants = computed(() =>
    this.catalog()?.products.items.flatMap((product) =>
      product.variants
        .filter((variant) => variant.isActive)
        .map((variant) => ({ productName: product.name, variant }))
    ) ?? []
  );

  protected readonly productForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] })
  });
  protected readonly variantForm = new FormGroup({
    productId: new FormControl<number | null>(null, Validators.required),
    sku: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    uomCode: new FormControl('', { nonNullable: true, validators: Validators.required }),
    uomFactor: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    basePrice: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] })
  });
  protected readonly ruleForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
    priority: new FormControl(10, { nonNullable: true, validators: [Validators.required, Validators.min(0), Validators.max(10000)] }),
    isStackable: new FormControl(true, { nonNullable: true }),
    appliesToAllVariants: new FormControl(false, { nonNullable: true }),
    conditions: new FormArray<FormGroup>([]),
    actions: new FormArray<FormGroup>([this.createActionGroup()])
  });

  constructor() {
    this.loadCatalog();
  }

  protected createProduct(): void {
    if (this.productForm.invalid || this.productSaving()) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.beginRequest();
    this.productSaving.set(true);
    this.apiService.createAdminProduct(this.productForm.controls.name.value)
      .pipe(
        finalize(() => this.productSaving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (product) => {
          this.catalog.update((catalog) => catalog ? {
            ...catalog,
            products: this.updatePage(
              catalog.products,
              this.sortProducts([...catalog.products.items, product]),
              catalog.products.totalCount + 1)
          } : catalog);
          this.productForm.reset();
          this.variantForm.controls.productId.setValue(product.id);
          this.confirmation.set(`${product.name} was added to the catalog.`);
        },
        error: () => this.error.set('The product could not be added. Check the name and try again.')
      });
  }

  protected createVariant(): void {
    if (this.variantForm.invalid || this.variantSaving()) {
      this.variantForm.markAllAsTouched();
      return;
    }

    const values = this.variantForm.getRawValue();
    if (values.productId === null) {
      return;
    }

    this.beginRequest();
    this.variantSaving.set(true);
    this.apiService.createProductVariant(values.productId, {
      sku: values.sku,
      uomCode: values.uomCode,
      uomFactor: values.uomFactor,
      basePrice: values.basePrice
    })
      .pipe(
        finalize(() => this.variantSaving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (variant) => {
          this.addVariantToCatalog(values.productId!, variant);
          this.variantForm.patchValue({ sku: '', uomFactor: 1, basePrice: 0 });
          this.confirmation.set(`${variant.sku} is now available to order.`);
        },
        error: () => this.error.set('The variant could not be added. SKU values must be unique and the unit must be valid.')
      });
  }

  protected changeVariantStatus(variant: AdminProductVariant): void {
    if (this.changingVariantIds().has(variant.id)) {
      return;
    }

    this.beginRequest();
    this.changingVariantIds.update((variantIds) => new Set(variantIds).add(variant.id));
    this.apiService.setProductVariantStatus(variant.id, !variant.isActive)
      .pipe(
        finalize(() => this.changingVariantIds.update((variantIds) => {
          const nextIds = new Set(variantIds);
          nextIds.delete(variant.id);
          return nextIds;
        })),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updatedVariant) => {
          this.replaceVariantInCatalog(updatedVariant);
          this.selectedVariantIds.update((variantIds) => {
            const nextIds = new Set(variantIds);
            nextIds.delete(updatedVariant.id);
            return nextIds;
          });
          this.confirmation.set(`${updatedVariant.sku} is ${updatedVariant.isActive ? 'enabled' : 'disabled'}.`);
        },
        error: () => this.error.set('The variant status could not be changed. Refresh the catalog and try again.')
      });
  }

  protected addCondition(): void {
    this.ruleForm.controls.conditions.push(this.createConditionGroup());
  }

  protected removeCondition(index: number): void {
    this.ruleForm.controls.conditions.removeAt(index);
  }

  protected addAction(): void {
    this.ruleForm.controls.actions.push(this.createActionGroup());
  }

  protected removeAction(index: number): void {
    if (this.ruleForm.controls.actions.length > 1) {
      this.ruleForm.controls.actions.removeAt(index);
    }
  }

  protected toggleVariantTarget(variantId: number, isSelected: boolean): void {
    this.selectedVariantIds.update((variantIds) => {
      const nextIds = new Set(variantIds);
      isSelected ? nextIds.add(variantId) : nextIds.delete(variantId);
      return nextIds;
    });
  }

  protected toggleCustomerTier(customerTier: CustomerTier, isSelected: boolean): void {
    this.selectedCustomerTiers.update((customerTiers) => {
      const nextTiers = new Set(customerTiers);
      isSelected ? nextTiers.add(customerTier) : nextTiers.delete(customerTier);
      return nextTiers;
    });
  }

  protected normalizeConditionOperator(condition: FormGroup): void {
    const attribute = condition.controls['attribute'].value as RuleAttribute;
    const operator = condition.controls['operator'].value as string;
    if (!this.conditionOperators(attribute).includes(operator)) {
      condition.controls['operator'].setValue(this.conditionOperators(attribute)[0]);
    }
  }

  protected conditionOperators(attribute: RuleAttribute): readonly string[] {
    return attribute === 'CustomerTier'
      ? ['=']
      : attribute === 'OrderDate'
        ? ['>=', '>', '<=', '<', '=', 'BETWEEN']
        : ['>=', '>', '<=', '<', '='];
  }

  protected conditionValuePlaceholder(attribute: RuleAttribute, operator: string): string {
    if (attribute === 'CustomerTier') {
      return 'VIP';
    }

    if (attribute === 'OrderDate') {
      return operator === 'BETWEEN' ? '2026-09-01 AND 2026-09-30' : '2026-09-01';
    }

    return attribute === 'Quantity' ? '3' : '25.00';
  }

  protected createPriceRule(): void {
    const formValues = this.ruleForm.getRawValue();
    if (this.ruleForm.invalid || this.ruleSaving() || (!formValues.appliesToAllVariants && this.selectedVariantIds().size === 0)) {
      this.ruleForm.markAllAsTouched();
      if (!formValues.appliesToAllVariants && this.selectedVariantIds().size === 0) {
        this.error.set('Select a product variant or apply the rule to all variants.');
      }
      return;
    }

    const request: CreatePriceRuleRequest = {
      name: formValues.name,
      priority: formValues.priority,
      isStackable: formValues.isStackable,
      appliesToAllVariants: formValues.appliesToAllVariants,
      variantIds: [...this.selectedVariantIds()],
      customerTiers: [...this.selectedCustomerTiers()],
      conditions: formValues.conditions.map((condition) => ({
        attribute: condition['attribute'] as string,
        operator: condition['operator'] as string,
        value: condition['value'] as string
      })),
      actions: formValues.actions.map((action) => ({
        actionType: action['actionType'] as ActionType,
        amount: action['amount'] as number,
        calculationBase: action['calculationBase'] as CalculationBase
      }))
    };

    this.beginRequest();
    this.ruleSaving.set(true);
    this.apiService.createPriceRule(request)
      .pipe(
        finalize(() => this.ruleSaving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (rule) => {
          this.catalog.update((catalog) => catalog ? {
            ...catalog,
            priceRules: this.updatePage(
              catalog.priceRules,
              this.sortRules([...catalog.priceRules.items, rule]),
              catalog.priceRules.totalCount + 1)
          } : catalog);
          this.resetRuleForm();
          this.confirmation.set(`${rule.name} is active.`);
        },
        error: () => this.error.set('The price rule could not be added. Check its targets, conditions, and actions.')
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

  protected actionBaseLabel(action: PriceRuleAction): string {
    return action.calculationBase === 'OriginalBase' ? 'base price' : 'running total';
  }

  protected changeProductPage(page: number): void {
    const catalog = this.catalog();
    if (!catalog || page < 1 || page > catalog.products.totalPages || page === this.productPage()) {
      return;
    }

    this.productPage.set(page);
    this.loadCatalog();
  }

  protected changePriceRulePage(page: number): void {
    const catalog = this.catalog();
    if (!catalog || page < 1 || page > catalog.priceRules.totalPages || page === this.priceRulePage()) {
      return;
    }

    this.priceRulePage.set(page);
    this.loadCatalog();
  }

  private loadCatalog(): void {
    this.catalogLoading.set(true);
    this.apiService.getAdminCatalog(this.productPage(), this.priceRulePage())
      .pipe(
        finalize(() => this.catalogLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (catalog) => {
          this.catalog.set(catalog);
          this.productPage.set(catalog.products.page);
          this.priceRulePage.set(catalog.priceRules.page);
        },
        error: () => this.error.set('The catalog could not be loaded. Refresh the page to try again.')
      });
  }

  private createConditionGroup(): FormGroup {
    return new FormGroup({
      attribute: new FormControl<RuleAttribute>('Quantity', { nonNullable: true, validators: Validators.required }),
      operator: new FormControl('>=', { nonNullable: true, validators: Validators.required }),
      value: new FormControl('', { nonNullable: true, validators: Validators.required })
    });
  }

  private createActionGroup(): FormGroup {
    return new FormGroup({
      actionType: new FormControl<ActionType>('PercentageDiscount', { nonNullable: true, validators: Validators.required }),
      amount: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
      calculationBase: new FormControl<CalculationBase>('RunningTotal', { nonNullable: true, validators: Validators.required })
    });
  }

  private resetRuleForm(): void {
    this.ruleForm.reset({ name: '', priority: 10, isStackable: true, appliesToAllVariants: false });
    this.ruleForm.controls.conditions.clear();
    this.ruleForm.controls.actions.clear();
    this.ruleForm.controls.actions.push(this.createActionGroup());
    this.selectedVariantIds.set(new Set());
    this.selectedCustomerTiers.set(new Set());
  }

  private beginRequest(): void {
    this.error.set(null);
    this.confirmation.set(null);
  }

  private addVariantToCatalog(productId: number, variant: AdminProductVariant): void {
    this.catalog.update((catalog) => catalog ? {
      ...catalog,
      products: this.updatePage(catalog.products, catalog.products.items.map((product) => product.id === productId
        ? { ...product, variants: [...product.variants, variant].sort((first, second) => first.sku.localeCompare(second.sku)) }
        : product))
    } : catalog);
  }

  private replaceVariantInCatalog(updatedVariant: AdminProductVariant): void {
    this.catalog.update((catalog) => catalog ? {
      ...catalog,
      products: this.updatePage(catalog.products, catalog.products.items.map((product) => ({
        ...product,
        variants: product.variants.map((variant) => variant.id === updatedVariant.id ? updatedVariant : variant)
      })))
    } : catalog);
  }

  private sortProducts(products: AdminProduct[]): AdminProduct[] {
    return products.sort((first, second) => first.name.localeCompare(second.name) || first.id - second.id);
  }

  private sortRules(rules: AdminPriceRule[]): AdminPriceRule[] {
    return rules.sort((first, second) => second.priority - first.priority || first.name.localeCompare(second.name));
  }

  private updatePage<T>(page: PagedResult<T>, items: T[], totalCount = page.totalCount): PagedResult<T> {
    return {
      ...page,
      items,
      totalCount,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / page.pageSize)
    };
  }
}