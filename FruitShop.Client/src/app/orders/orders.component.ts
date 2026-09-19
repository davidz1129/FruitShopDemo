import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';
import { finalize } from 'rxjs';
import { OrderSummaryComponent, OrderSummaryLine } from '../shared/order-summary.component';
import { TopLinksBarComponent } from '../shared/top-links-bar.component';
import { ApiService, OrderListItem, OrderResponse } from '../services/api.service';

@Component({
  selector: 'app-orders',
  imports: [DatePipe, LucideChevronLeft, LucideChevronRight, OrderSummaryComponent, TopLinksBarComponent],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrdersComponent {
  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly error = signal<string | null>(null);
  protected readonly orders = signal<OrderListItem[]>([]);
  protected readonly ordersPage = signal(1);
  protected readonly ordersTotalPages = signal(0);
  protected readonly ordersLoading = signal(true);
  protected readonly selectedOrder = signal<OrderResponse | null>(null);
  protected readonly selectedOrderLoading = signal(false);
  protected readonly summaryLines = computed<readonly OrderSummaryLine[]>(() =>
    this.selectedOrder()?.items.map((item) => ({
      variantId: item.variantId,
      sku: item.sku,
      unitOfMeasure: item.unitOfMeasure,
      quantity: item.quantity,
      unitPriceApplied: item.unitPriceApplied,
      lineSubtotal: item.totalLineAmount,
      priceChangeReason: item.priceChangeReason
    })) ?? []
  );

  constructor() {
    this.loadOrders();
  }

  protected selectOrder(orderId: number): void {
    if (this.selectedOrder()?.id === orderId || this.selectedOrderLoading()) {
      return;
    }

    this.error.set(null);
    this.selectedOrder.set(null);
    this.selectedOrderLoading.set(true);
    this.apiService.getOrder(orderId)
      .pipe(
        finalize(() => this.selectedOrderLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (order) => this.selectedOrder.set(order),
        error: () => this.error.set('The selected order could not be loaded. Choose another order to try again.')
      });
  }

  protected changeOrdersPage(page: number): void {
    if (page < 1 || page > this.ordersTotalPages() || page === this.ordersPage()) {
      return;
    }

    this.ordersPage.set(page);
    this.selectedOrder.set(null);
    this.loadOrders();
  }

  private loadOrders(): void {
    this.ordersLoading.set(true);
    this.apiService.getOrders(this.ordersPage())
      .pipe(
        finalize(() => this.ordersLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (orders) => {
          this.orders.set(orders.items);
          this.ordersPage.set(orders.page);
          this.ordersTotalPages.set(orders.totalPages);
          if (orders.items.length > 0) {
            this.selectOrder(orders.items[0].id);
          }
        },
        error: () => this.error.set('Existing orders could not be loaded. Refresh the page to try again.')
      });
  }
}