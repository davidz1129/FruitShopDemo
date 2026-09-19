import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ViewEncapsulation, input, output } from '@angular/core';
import { LucidePackagePlus, LucideShoppingBasket, LucideTrash } from '@lucide/angular';

export interface OrderSummaryLine {
  variantId: number;
  sku: string;
  unitOfMeasure: string;
  quantity: number;
  unitPriceApplied: number;
  lineSubtotal: number;
  priceChangeReason: string;
}

@Component({
  selector: 'app-order-summary',
  imports: [CurrencyPipe, DecimalPipe, LucidePackagePlus, LucideShoppingBasket, LucideTrash],
  templateUrl: './order-summary.component.html',
  styleUrl: './order-summary.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderSummaryComponent {
  readonly actionDisabled = input(false);
  readonly actionLabel = input<string | null>(null);
  readonly eyebrow = input('Not yet saved');
  readonly emptyMessage = input('Calculated items will stay here until you submit the order.');
  readonly lines = input.required<readonly OrderSummaryLine[]>();
  readonly removable = input(false);
  readonly title = input.required<string>();
  readonly totalLabel = input('Draft total');

  readonly actionClicked = output<void>();
  readonly lineRemoved = output<number>();

  protected total(): number {
    return this.lines().reduce((subtotal, line) => subtotal + line.lineSubtotal, 0);
  }
}