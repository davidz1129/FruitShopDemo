import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideChevronLeft, LucideChevronRight, LucidePackagePlus } from '@lucide/angular';
import { EMPTY, catchError, debounceTime, distinctUntilChanged, finalize, map, startWith, switchMap } from 'rxjs';
import { OrderSummaryComponent } from '../shared/order-summary.component';
import { TopLinksBarComponent } from '../shared/top-links-bar.component';
import { ApiService, CustomerTier, OrderItemPreview, OrderResponse, PriceRuleAction, PriceRuleCondition, Product, ProductVariant } from '../services/api.service';

@Component({
  selector: 'app-new-order',
  imports: [CurrencyPipe, DecimalPipe, LucideChevronLeft, LucideChevronRight, LucidePackagePlus, OrderSummaryComponent, ReactiveFormsModule, TopLinksBarComponent],
  templateUrl: './new-order.component.html',
  styleUrl: '../app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewOrderComponent {
  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly customerTiers: CustomerTier[] = ['RETAIL', 'VIP', 'WHOLESALE'];
  protected readonly products = signal<Product[]>([]);
  protected readonly productPage = signal(1);
  protected readonly productTotalPages = signal(0);
  protected readonly productVariantGroups = computed(() => {
    const productsByName = new Map<string, Product>();

    for (const product of this.products()) {
      const existingProduct = productsByName.get(product.name);
      if (existingProduct) {
        existingProduct.variants.push(...product.variants);
      } else {
        productsByName.set(product.name, { ...product, variants: [...product.variants] });
      }
    }

    return [...productsByName.values()];
  });
  protected readonly preview = signal<OrderItemPreview | null>(null);
  protected readonly draftLines = signal<OrderItemPreview[]>([]);
  protected readonly productsLoading = signal(true);
  protected readonly calculating = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly confirmation = signal<OrderResponse | null>(null);

  protected readonly itemForm = new FormGroup({
    customerTier: new FormControl<CustomerTier | null>(null, Validators.required),
    variantId: new FormControl<number | null>({ value: null, disabled: true }, Validators.required),
    quantity: new FormControl({ value: 1, disabled: true }, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.0001)]
    })
  });

  constructor() {
    this.loadProducts();
    this.itemForm.controls.customerTier.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.preview.set(null);
        this.updateFormControlState();
      });
    this.itemForm.controls.variantId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((variantId) => {
        this.preview.set(null);
        this.restoreDraftQuantity(variantId);
        this.updateFormControlState();
      });
    this.itemForm.valueChanges
      .pipe(
        startWith(null),
        map(() => this.itemForm.getRawValue()),
        debounceTime(180),
        distinctUntilChanged(
          (previous, current) =>
            previous.customerTier === current.customerTier &&
            previous.variantId === current.variantId &&
            previous.quantity === current.quantity
        ),
        switchMap((value) => this.calculatePreview(value)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((preview) => this.preview.set(preview));
  }

  protected addToDraft(): void {
    const preview = this.preview();
    const { variantId, quantity } = this.itemForm.getRawValue();
    if (!preview || this.calculating() || !this.isValidQuantity(quantity) || preview.variantId !== variantId || preview.quantity !== quantity) {
      return;
    }

    this.draftLines.update((lines) => {
      const existingLineIndex = lines.findIndex((line) => line.variantId === preview.variantId);

      return existingLineIndex === -1
        ? [...lines, preview]
        : lines.map((line, index) => index === existingLineIndex ? preview : line);
    });
    this.preview.set(null);
    this.itemForm.patchValue({ variantId: null, quantity: 1 });
    this.updateFormControlState();
  }

  protected removeDraftLine(index: number): void {
    this.draftLines.update((lines) => lines.filter((_, lineIndex) => lineIndex !== index));
    this.updateFormControlState();
    this.refreshPreview();
  }

  protected submitOrder(): void {
    const customerTier = this.itemForm.controls.customerTier.value;
    const draftLines = this.draftLines();

    if (!customerTier || draftLines.length === 0 || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.itemForm.disable({ emitEvent: false });

    this.apiService.submitOrder(
      customerTier,
      draftLines.map((line) => ({ variantId: line.variantId, quantity: line.quantity }))
    )
      .pipe(
        finalize(() => {
          this.submitting.set(false);
          this.updateFormControlState();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (order) => {
          this.confirmation.set(order);
          this.draftLines.set([]);
          this.preview.set(null);
          this.itemForm.reset({ customerTier: null, variantId: null, quantity: 1 });
        },
        error: () => this.error.set('The order was not submitted. Review the selected tier and items, then try again.')
      });
  }

  protected selectedVariant(): ProductVariant | null {
    const variantId = this.itemForm.controls.variantId.value;
    if (variantId === null) {
      return null;
    }

    return this.products()
      .flatMap((product) => product.variants)
      .find((variant) => variant.id === variantId) ?? null;
  }

  protected selectedDraftLine(): OrderItemPreview | null {
    const variantId = this.itemForm.controls.variantId.value;
    if (variantId === null) {
      return null;
    }

    return this.draftLines().find((line) => line.variantId === variantId) ?? null;
  }

  protected customerTierLocked(): boolean {
    return this.draftLines().length > 0 || this.submitting();
  }

  protected quantityMinimum(): number {
    return this.isWeightMeasure() ? 0.01 : 1;
  }

  protected quantityStep(): number {
    return this.isWeightMeasure() ? 0.01 : 1;
  }

  protected quantityInputMode(): 'decimal' | 'numeric' {
    return this.isWeightMeasure() ? 'decimal' : 'numeric';
  }

  protected quantityIsValid(): boolean {
    return this.isValidQuantity(this.itemForm.controls.quantity.value);
  }

  protected quantityValidationMessage(): string {
    return this.isWeightMeasure()
      ? 'Weight quantities support up to two decimal places.'
      : 'Count quantities must be whole numbers.';
  }

  protected changeProductPage(page: number): void {
    if (page < 1 || page > this.productTotalPages() || page === this.productPage()) {
      return;
    }

    this.itemForm.patchValue({ variantId: null, quantity: 1 });
    this.productPage.set(page);
    this.loadProducts();
  }

  protected describeCondition(condition: PriceRuleCondition): string {
    const operatorLabels: Record<string, string> = {
      GreaterThan: 'is greater than',
      GreaterThanOrEqual: 'is at least',
      LessThan: 'is less than',
      LessThanOrEqual: 'is at most',
      '=': 'is'
    };

    return `${condition.attribute} ${operatorLabels[condition.operator] ?? condition.operator} ${condition.value}`;
  }

  protected describeAction(action: PriceRuleAction): string {
    const calculationBase = action.calculationBase === 'OriginalBase' ? 'from base price' : 'from current price';

    switch (action.actionType) {
      case 'PercentageDiscount':
        return `${action.amount}% off ${calculationBase}`;
      case 'AmountOff':
        return `$${action.amount.toFixed(2)} off ${calculationBase}`;
      case 'OverridePrice':
        return `Set price to $${action.amount.toFixed(2)}`;
      default:
        return action.actionType;
    }
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
        error: () => this.error.set('The product catalog could not be loaded. Refresh the page to try again.')
      });
  }

  private calculatePreview(value: { customerTier: CustomerTier | null; variantId: number | null; quantity: number }) {
    if (!value.customerTier || !value.variantId || !this.isPreviewableQuantity(value.quantity)) {
      this.preview.set(null);
      this.calculating.set(false);
      return EMPTY;
    }

    const variant = this.findVariant(value.variantId);
    if (!variant) {
      this.preview.set(null);
      return EMPTY;
    }

    this.preview.set(null);
    this.calculating.set(true);
    this.error.set(null);

    return this.apiService.calculateOrderItem({
      customerTier: value.customerTier,
      variantId: value.variantId,
      quantity: value.quantity,
      cartSubtotal: this.draftSubtotalExcluding(value.variantId) + (variant.basePrice * value.quantity)
    }).pipe(
      finalize(() => this.calculating.set(false)),
      catchError(() => {
        this.error.set('The item could not be priced. Check the selected tier, item, and quantity.');
        return EMPTY;
      })
    );
  }

  private findVariant(variantId: number): ProductVariant | null {
    return this.products()
      .flatMap((product) => product.variants)
      .find((variant) => variant.id === variantId) ?? null;
  }

  private restoreDraftQuantity(variantId: number | null): void {
    if (variantId === null) {
      return;
    }

    const existingLine = this.draftLines().find((line) => line.variantId === variantId);
    this.itemForm.controls.quantity.setValue(existingLine?.quantity ?? 0);
  }

  private draftSubtotalExcluding(variantId: number): number {
    return this.draftLines()
      .filter((line) => line.variantId !== variantId)
      .reduce((subtotal, line) => subtotal + line.lineSubtotal, 0);
  }

  private updateFormControlState(): void {
    const { customerTier, variantId } = this.itemForm.getRawValue();
    const customerTierLocked = this.customerTierLocked();

    if (customerTierLocked) {
      this.itemForm.controls.customerTier.disable({ emitEvent: false });
    } else {
      this.itemForm.controls.customerTier.enable({ emitEvent: false });
    }

    if (customerTier && !this.submitting()) {
      this.itemForm.controls.variantId.enable({ emitEvent: false });
    } else {
      this.itemForm.controls.variantId.disable({ emitEvent: false });
    }

    if (customerTier && variantId && !this.submitting()) {
      this.itemForm.controls.quantity.enable({ emitEvent: false });
    } else {
      this.itemForm.controls.quantity.disable({ emitEvent: false });
    }
  }

  private refreshPreview(): void {
    this.calculatePreview(this.itemForm.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((preview) => this.preview.set(preview));
  }

  private isValidQuantity(quantity: number): boolean {
    if (!Number.isFinite(quantity) || quantity <= 0) {
      return false;
    }

    if (this.isWeightMeasure()) {
      return Math.abs((quantity * 100) - Math.round(quantity * 100)) < 0.0000001;
    }

    return Number.isInteger(quantity);
  }

  private isPreviewableQuantity(quantity: number): boolean {
    return quantity === 0 || this.isValidQuantity(quantity);
  }

  private isWeightMeasure(): boolean {
    return this.selectedVariant()?.unitOfMeasure.measureType === 'Weight';
  }
}