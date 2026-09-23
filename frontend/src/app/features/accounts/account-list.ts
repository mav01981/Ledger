import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { AccountStore } from '../../core/account-store';
import { AccountType } from '../../core/ledger.model';
import { AccountCard } from './components/account-card';

@Component({
  selector: 'app-account-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AccountCard],
  templateUrl: './account-list.html',
  styleUrl: './account-list.css',
})
export class AccountList {
  protected readonly store = inject(AccountStore);
  protected readonly AccountType = AccountType;
  protected readonly newAccountType = signal<AccountType>(AccountType.Standard);
  protected readonly opening = signal(false);

  constructor() {
    void this.store.loadAccounts();
  }

  protected onTypeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.newAccountType.set(value as AccountType);
  }

  protected retryLoad(): void {
    void this.store.loadAccounts();
  }

  protected async submitOpenAccount(event: Event): Promise<void> {
    event.preventDefault();
    if (this.opening()) return;
    this.opening.set(true);
    try {
      await this.store.openAccount(this.newAccountType());
    } finally {
      this.opening.set(false);
    }
  }
}
