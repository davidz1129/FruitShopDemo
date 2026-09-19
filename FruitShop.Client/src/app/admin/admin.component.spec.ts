import { WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AdminComponent } from './admin.component';
import { AdminCatalog, AdminProduct, ApiService, CreatePriceRuleRequest } from '../services/api.service';

describe('AdminComponent', () => {
  const catalog: AdminCatalog = {
    products: {
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
          measureType: 'Weight'
        }]
      }],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1
    },
    unitsOfMeasure: [{ code: 'kg', measureType: 'Weight' }],
    priceRules: { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 }
  };
  let productRequests: string[];
  let ruleRequests: CreatePriceRuleRequest[];

  beforeEach(async () => {
    productRequests = [];
    ruleRequests = [];
    await TestBed.configureTestingModule({
      imports: [AdminComponent],
      providers: [
        provideRouter([]),
        {
          provide: ApiService,
          useValue: {
            getAdminCatalog: () => of(catalog),
            createAdminProduct: (name: string) => {
              productRequests.push(name);
              return of<AdminProduct>({ id: 2, name, variants: [] });
            },
            createPriceRule: (request: CreatePriceRuleRequest) => {
              ruleRequests.push(request);
              return of({
                id: 20,
                ...request,
                status: 'Active' as const,
                targetVariantIds: request.variantIds,
                targetCustomerTiers: request.customerTiers
              });
            }
          }
        }
      ]
    }).compileComponents();
  });

  it('adds a product and selects it for the next variant', () => {
    const fixture = TestBed.createComponent(AdminComponent);
    const component = fixture.componentInstance as unknown as {
      productForm: { controls: { name: { setValue(value: string): void } } };
      variantForm: { controls: { productId: { value: number | null } } };
      catalog: WritableSignal<AdminCatalog | null>;
      createProduct(): void;
    };

    component.productForm.controls.name.setValue('Black plums');
    component.createProduct();

    expect(productRequests).toEqual(['Black plums']);
    expect(component.variantForm.controls.productId.value).toBe(2);
    expect(component.catalog()?.products.items.map((product) => product.name)).toEqual(['Black plums', 'Test apples']);
  });

  it('creates a targeted rule with its price action', () => {
    const fixture = TestBed.createComponent(AdminComponent);
    const component = fixture.componentInstance as unknown as {
      ruleForm: {
        controls: {
          name: { setValue(value: string): void };
          priority: { setValue(value: number): void };
          actions: {
            at(index: number): {
              controls: {
                actionType: { setValue(value: 'PercentageDiscount'): void };
                amount: { setValue(value: number): void };
                calculationBase: { setValue(value: 'RunningTotal'): void };
              };
            };
          };
        };
      };
      selectedVariantIds: WritableSignal<ReadonlySet<number>>;
      selectedCustomerTiers: WritableSignal<ReadonlySet<'VIP'>>;
      createPriceRule(): void;
    };

    component.ruleForm.controls.name.setValue('VIP apple discount');
    component.ruleForm.controls.priority.setValue(25);
    component.ruleForm.controls.actions.at(0).controls.actionType.setValue('PercentageDiscount');
    component.ruleForm.controls.actions.at(0).controls.amount.setValue(15);
    component.ruleForm.controls.actions.at(0).controls.calculationBase.setValue('RunningTotal');
    component.selectedVariantIds.set(new Set([11]));
    component.selectedCustomerTiers.set(new Set(['VIP']));
    component.createPriceRule();

    expect(ruleRequests).toEqual([{
      name: 'VIP apple discount',
      priority: 25,
      isStackable: true,
      appliesToAllVariants: false,
      variantIds: [11],
      customerTiers: ['VIP'],
      conditions: [],
      actions: [{ actionType: 'PercentageDiscount', amount: 15, calculationBase: 'RunningTotal' }]
    }]);
  });
});