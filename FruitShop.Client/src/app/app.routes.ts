import { Routes } from '@angular/router';

export const routes: Routes = [
	{
		path: '',
		pathMatch: 'full',
		loadComponent: () => import('./new-order/new-order.component').then((component) => component.NewOrderComponent),
		title: 'FruitShop | New order'
	},
	{
		path: 'orders',
		loadComponent: () => import('./orders/orders.component').then((component) => component.OrdersComponent),
		title: 'FruitShop | Orders'
	},
	{
		path: 'admin',
		loadComponent: () => import('./admin/admin.component').then((component) => component.AdminComponent),
		title: 'FruitShop | Catalog administration'
	},
	{
		path: 'product-variant-price-rules',
		loadComponent: () => import('./product-variant-price-rules/product-variant-price-rules.component').then((component) => component.ProductVariantPriceRulesComponent),
		title: 'FruitShop | Product variant price rules'
	},
	{ path: '**', redirectTo: '' }
];
