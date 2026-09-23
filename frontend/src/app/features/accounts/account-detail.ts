import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AccountStore } from '../../core/account-store';
import { BalanceDisplay } from './components/balance-display';
import { ConsistencyGapPanel } from './components/consistency-gap-panel';
import { ReplayScrubber } from './components/replay-scrubber';
import { TransactionForm } from './components/transaction-form';
import { TransactionHistory } from './components/transaction-history';

/**
 * Container for one account: orchestrates the balance display, replay
 * scrubber, event stream, write form, and consistency-gap panel. Owns the
 * polling lifecycle (start on activation, stop when destroyed) per spec.
 */
@Component({
  selector: 'app-account-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    BalanceDisplay,
    ConsistencyGapPanel,
    ReplayScrubber,
    TransactionForm,
    TransactionHistory,
  ],
  templateUrl: './account-detail.html',
  styleUrl: './account-detail.css',
})
export class AccountDetail {
  /** Bound from the :id route parameter via withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly store = inject(AccountStore);

  protected readonly minDate = computed(() => {
    const history = this.store.history();
    if (history && history.length > 0) return new Date(history[0].timestamp);
    const account = this.store.selectedAccount();
    return account ? new Date(account.openedAt) : new Date();
  });

  protected readonly scrubValue = computed(() => {
    const scrubbed = this.store.scrubberBalance();
    return scrubbed ? new Date(scrubbed.asOf) : this.store.scrubberMax();
  });

  protected readonly displayBalance = computed(() => {
    const scrubbed = this.store.scrubberBalance();
    if (scrubbed) return scrubbed.balance;
    return this.store.selectedAccount()?.balance ?? null;
  });

  protected readonly displayAsOf = computed(() => this.store.scrubberBalance()?.asOf ?? null);

  constructor() {
    // Transfer targets need the full account list even on a deep link.
    void this.store.loadAccounts();
    effect((onCleanup) => {
      const id = this.id();
      void this.store.selectAccount(id);
      this.store.startPolling(id);
      onCleanup(() => this.store.stopPolling());
    });
  }

  protected onScrub(asOf: Date): void {
    void this.store.scrubTo(asOf);
  }

  protected backToNow(): void {
    this.store.clearScrub();
  }

  protected retryConnection(): void {
    this.store.retryConnection();
  }
}
