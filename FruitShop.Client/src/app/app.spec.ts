import { WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NewOrderComponent } from './new-order/new-order.component';
import { ApiService, CustomerTier, OrderItemPreview, Product } from './services/api.service';

describe('NewOrderComponent', () => {
  let calculationRequests: Array<{
    customerTier: CustomerTier;
    variantId: number;
    quantity: number;
    cartSubtotal: number;
  }>;

  beforeEach(async () => {
    calculationRequests = [];
    await TestBed.configureTestingModule({
      imports: [NewOrderComponent],
      providers: [
        provideRouter([]),
        {
          provide: ApiService,
          useValue: {
            getProducts: () => of({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 }),
            calculateOrderItem: (request: {
              customerTier: CustomerTier;
              variantId: number;
              quantity: number;
              cartSubtotal: number;
            }) => {
              calculationRequests.push(request);
              return of<OrderItemPreview>({
                variantId: request.variantId,
                sku: 'TEST-VARIANT',
                unitOfMeasure: 'each',
                baseUnitPrice: 2,
                quantity: request.quantity,
                unitPriceApplied: 2,
                lineSubtotal: request.quantity * 2,
                priceChangeReason: 'Base price applied',
                activePriceRules: [{
                  id: 1,
                  name: 'Test active rule',
                  priority: 1,
                  isStackable: false,
                  conditions: [],
                  actions: []
                }]
              });
            }
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(NewOrderComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('groups variants from products with the same name', () => {
    const fixture = TestBed.createComponent(NewOrderComponent);
    const component = fixture.componentInstance as unknown as {
      products: WritableSignal<Product[]>;
    };

    component.products.set([
      {
        id: 1,
        name: 'Cherry',
        variants: [{ id: 1, sku: 'CHERRY-1KG', uomCode: 'kg', uomFactor: 1, basePrice: 5, unitOfMeasure: { code: 'kg', measureType: 'Weight' } }]
      },
      {
        id: 2,
        name: 'Cherry',
        variants: [{ id: 2, sku: 'CHERRY-5KG-BOX', uomCode: 'box', uomFactor: 5, basePrice: 45, unitOfMeasure: { code: 'box', measureType: 'Count' } }]
      }
    ]);
    fixture.detectChanges();

    const productGroups = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('optgroup'));

    expect(productGroups).toHaveLength(1);
    expect(productGroups[0].label).toBe('Cherry');
    expect(Array.from(productGroups[0].querySelectorAll('option'), (option) => option.text)).toEqual([
      'CHERRY-1KG - $5.00/kg',
      'CHERRY-5KG-BOX - $45.00/box'
    ]);
  });

  it('updates an existing draft line when its variant is selected again', () => {
    const fixture = TestBed.createComponent(NewOrderComponent);
    const app = fixture.componentInstance;
    const component = app as unknown as {
      draftLines: WritableSignal<OrderItemPreview[]>;
      preview: WritableSignal<OrderItemPreview | null>;
      itemForm: {
        controls: {
          customerTier: { setValue(value: 'RETAIL'): void };
          variantId: { setValue(value: number): void };
          quantity: { value: number; setValue(value: number): void };
        };
      };
      addToDraft(): void;
    };
    const originalLine: OrderItemPreview = {
      variantId: 1,
      sku: 'PRODUCT-VARIANT-A',
      unitOfMeasure: 'kg',
      baseUnitPrice: 4,
      quantity: 2,
      unitPriceApplied: 4,
      lineSubtotal: 8,
      priceChangeReason: 'Base price applied',
      activePriceRules: []
    };
    const updatedLine: OrderItemPreview = {
      ...originalLine,
      quantity: 3,
      lineSubtotal: 12
    };

    component.draftLines.set([originalLine]);
  component.itemForm.controls.customerTier.setValue('RETAIL');
  component.itemForm.controls.variantId.setValue(originalLine.variantId);

  expect(component.itemForm.controls.quantity.value).toBe(originalLine.quantity);

  component.itemForm.controls.variantId.setValue(2);

  expect(component.itemForm.controls.quantity.value).toBe(0);

  component.itemForm.controls.variantId.setValue(originalLine.variantId);

  expect(component.itemForm.controls.quantity.value).toBe(originalLine.quantity);

  component.itemForm.controls.quantity.setValue(updatedLine.quantity);
    component.preview.set(updatedLine);
    component.addToDraft();

    expect(component.draftLines()).toEqual([updatedLine]);
  });

  it('shows active rules but not calculated pricing for zero quantity', () => {
    const fixture = TestBed.createComponent(NewOrderComponent);
    const component = fixture.componentInstance as unknown as {
      products: WritableSignal<Product[]>;
      preview: WritableSignal<OrderItemPreview | null>;
      itemForm: {
        controls: {
          customerTier: { setValue(value: CustomerTier): void };
          variantId: { setValue(value: number): void };
        };
      };
      calculatePreview(value: {
        customerTier: CustomerTier;
        variantId: number;
        quantity: number;
        cartSubtotal: number;
      }): { subscribe(callback: (preview: OrderItemPreview) => void): void };
    };

    component.products.set([{
      id: 1,
      name: 'Test product',
      variants: [{ id: 1, sku: 'TEST-VARIANT', uomCode: 'each', uomFactor: 1, basePrice: 2, unitOfMeasure: { code: 'each', measureType: 'Count' } }]
    }]);
    component.itemForm.controls.customerTier.setValue('RETAIL');
    component.itemForm.controls.variantId.setValue(1);
    component.calculatePreview({ customerTier: 'RETAIL', variantId: 1, quantity: 0, cartSubtotal: 0 })
      .subscribe((preview) => component.preview.set(preview));
    fixture.detectChanges();

    const content = (fixture.nativeElement as HTMLElement).textContent;

    expect(calculationRequests).toContainEqual(expect.objectContaining({ quantity: 0, variantId: 1 }));
    expect(content).toContain('Active price rules');
    expect(content).not.toContain('Calculated item subtotal');
  });

  it('uses quantity precision that matches the selected measure type', () => {
    const fixture = TestBed.createComponent(NewOrderComponent);
    const app = fixture.componentInstance;
    const component = app as unknown as {
      products: WritableSignal<Product[]>;
      itemForm: {
        controls: {
          customerTier: { setValue(value: 'RETAIL'): void };
          variantId: { setValue(value: number): void };
          quantity: { setValue(value: number): void };
        };
      };
      quantityIsValid(): boolean;
    };

    component.products.set([{
      id: 1,
      name: 'Test product',
      variants: [
        { id: 1, sku: 'WEIGHT-VARIANT', uomCode: 'kg', uomFactor: 1, basePrice: 4, unitOfMeasure: { code: 'kg', measureType: 'Weight' } },
        { id: 2, sku: 'COUNT-VARIANT', uomCode: 'each', uomFactor: 1, basePrice: 2, unitOfMeasure: { code: 'each', measureType: 'Count' } }
      ]
    }]);
    component.itemForm.controls.customerTier.setValue('RETAIL');
    component.itemForm.controls.variantId.setValue(1);
    fixture.detectChanges();

    let quantityInput = fixture.nativeElement.querySelector('#quantity') as HTMLInputElement;
    expect(quantityInput.min).toBe('0.01');
    expect(quantityInput.step).toBe('0.01');
    expect(quantityInput.inputMode).toBe('decimal');
    component.itemForm.controls.quantity.setValue(1.234);
    expect(component.quantityIsValid()).toBe(false);

    component.itemForm.controls.variantId.setValue(2);
    fixture.detectChanges();

    quantityInput = fixture.nativeElement.querySelector('#quantity') as HTMLInputElement;
    expect(quantityInput.min).toBe('1');
    expect(quantityInput.step).toBe('1');
    expect(quantityInput.inputMode).toBe('numeric');
    component.itemForm.controls.quantity.setValue(1.5);
    expect(component.quantityIsValid()).toBe(false);
  });
});
