import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'accounts' },
  {
    path: 'accounts',
    loadComponent: () => import('./features/accounts/account-list').then((m) => m.AccountList),
  },
  {
    path: 'accounts/:id',
    loadComponent: () => import('./features/accounts/account-detail').then((m) => m.AccountDetail),
  },
  { path: '**', redirectTo: 'accounts' },
];

