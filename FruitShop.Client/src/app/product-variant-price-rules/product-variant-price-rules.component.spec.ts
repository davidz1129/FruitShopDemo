import { WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProductVariantPriceRulesComponent } from './product-variant-price-rules.component';
import { ApiService, ProductVariantPriceRulesResponse } from '../services/api.service';

describe('ProductVariantPriceRulesComponent', () => {
  const priceRules: ProductVariantPriceRulesResponse = {
    variant: { id: 11, sku: 'TEST-APPLE-1KG', uomCode: 'kg', uomFactor: 1, basePrice: 4.5, isActive: true, measureType: 'Weight' },
    assignedPriceRules: [],
    availablePriceRules: [{
      id: 20,
      name: 'VIP apple discount',
      priority: 25,
      isStackable: true,
      appliesToAllVariants: false,
      status: 'Active',
      targetVariantIds: [],
      targetCustomerTiers: ['VIP'],
      conditions: [],
      actions: [{ actionType: 'PercentageDiscount', amount: 15, calculationBase: 'RunningTotal' }]
    }],
    globalPriceRules: []
  };
  let selectedVariantIds: number[];
  let assignedRuleIds: number[];
  let priceRulesResponse: ProductVariantPriceRulesResponse;

  beforeEach(async () => {
    selectedVariantIds = [];
    assignedRuleIds = [];
    priceRulesResponse = priceRules;
    await TestBed.configureTestingModule({
      imports: [ProductVariantPriceRulesComponent],
      providers: [
        provideRouter([]),
        {
          provide: ApiService,
          useValue: {
            getProducts: () => of({
              items: [{
                id: 1,
                name: 'Test apples',
                variants: [{
                  id: 11,
                  sku: 'TEST-APPLE-1KG',
                  uomCode: 'kg',
                  uomFactor: 1,
                  basePrice: 4.5,
                  isActive: true,
                  unitOfMeasure: { code: 'kg', measureType: 'Weight' },
                  activePriceRules: []
                }]
              }],
              page: 1,
              pageSize: 25,
              totalCount: 1,
              totalPages: 1
            }),
            getProductVariantPriceRules: (variantId: number) => {
              selectedVariantIds.push(variantId);
              return of(priceRulesResponse);
            },
            assignPriceRuleToProductVariant: (_variantId: number, priceRuleId: number) => {
              assignedRuleIds.push(priceRuleId);
              return of(priceRules.availablePriceRules[0]);
            },
            removePriceRuleFromProductVariant: () => of(void 0)
          }
        }
      ]
    }).compileComponents();
  });

  it('loads the selected variant price rules', () => {
    const fixture = TestBed.createComponent(ProductVariantPriceRulesComponent);
    const component = fixture.componentInstance as unknown as {
      selectVariant(variantId: number): void;
      selectedVariantId: WritableSignal<number | null>;
      priceRules: WritableSignal<ProductVariantPriceRulesResponse | null>;
    };

    component.selectVariant(11);

    expect(selectedVariantIds).toEqual([11]);
    expect(component.selectedVariantId()).toBe(11);
    expect(component.priceRules()?.variant.sku).toBe('TEST-APPLE-1KG');
  });

  it('assigns a price rule and refreshes the selected variant', () => {
    const fixture = TestBed.createComponent(ProductVariantPriceRulesComponent);
    const component = fixture.componentInstance as unknown as {
      selectVariant(variantId: number): void;
      assignPriceRule(priceRuleId: number): void;
    };

    component.selectVariant(11);
    component.assignPriceRule(20);

    expect(assignedRuleIds).toEqual([20]);
    expect(selectedVariantIds).toEqual([11, 11]);
  });

  it('renders rule details and commands when the response omits global rules', () => {
    const { globalPriceRules: _, ...legacyResponse } = priceRules;
    priceRulesResponse = {
      ...legacyResponse,
      assignedPriceRules: [priceRules.availablePriceRules[0]]
    } as ProductVariantPriceRulesResponse;
    const fixture = TestBed.createComponent(ProductVariantPriceRulesComponent);
    const component = fixture.componentInstance as unknown as {
      selectVariant(variantId: number): void;
    };

    component.selectVariant(11);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('VIP apple discount');
    expect(fixture.nativeElement.textContent).toContain('15% off');
    expect(fixture.nativeElement.querySelector('[aria-label="Remove VIP apple discount"]')).not.toBeNull();
  });
});