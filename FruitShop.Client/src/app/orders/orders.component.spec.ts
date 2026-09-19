import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { OrdersComponent } from './orders.component';
import { ApiService, OrderResponse } from '../services/api.service';

describe('OrdersComponent', () => {
  it('loads order numbers and displays the selected order with its items', async () => {
    const firstOrder: OrderResponse = {
      id: 42,
      customerTier: 'VIP',
      createdAt: '2026-09-18T10:30:00Z',
      items: [{
        id: 1,
        variantId: 7,
        sku: 'APPLE-HONEY-1KG',
        unitOfMeasure: 'kg',
        quantity: 2,
        unitPriceApplied: 2.99,
        totalLineAmount: 5.98,
        priceChangeReason: 'Season sale applied'
      }]
    };
    const secondOrder: OrderResponse = {
      ...firstOrder,
      id: 41,
      items: []
    };

    await TestBed.configureTestingModule({
      imports: [OrdersComponent],
      providers: [
        provideRouter([]),
        {
          provide: ApiService,
          useValue: {
            getOrders: () => of({
              items: [
                { id: firstOrder.id, createdAt: firstOrder.createdAt },
                { id: secondOrder.id, createdAt: secondOrder.createdAt }
              ],
              page: 1,
              pageSize: 25,
              totalCount: 2,
              totalPages: 1
            }),
            getOrder: (orderId: number) => of(orderId === firstOrder.id ? firstOrder : secondOrder)
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(OrdersComponent);
    fixture.detectChanges();

    const content = (fixture.nativeElement as HTMLElement).textContent;
    expect(content).toContain('Order #42');
    expect(content).toContain('APPLE-HONEY-1KG');
    expect(content).toContain('$5.98');
  });
});