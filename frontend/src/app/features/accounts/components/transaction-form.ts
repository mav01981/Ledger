import { DecimalPipe, SlicePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';

import { AccountStore } from '../../../core/account-store';
import { Account } from '../../../core/ledger.model';

type TransactionMode = 'deposit' | 'withdraw' | 'transfer';

/**
 * One form with a mode toggle. Errors are surfaced inline from the API's own
 * rejection body — explaining *why* an event was rejected is part of the demo.
 * The selected account is always the transfer source, so the consistency-gap
 * version comparison stays on one aggregate.
 */
@Component({
  selector: 'app-transaction-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, SlicePipe],
  templateUrl: './transaction-form.html',
  styleUrl: './transaction-form.css',
})
export class TransactionForm {
  readonly account = input.required<Account>();
  readonly accounts = input<Account[]>([]);

  protected readonly store = inject(AccountStore);
  protected readonly modes: TransactionMode[] = ['deposit', 'withdraw', 'transfer'];
  protected readonly mode = signal<TransactionMode>('deposit');
  protected readonly amount = signal('');
  protected readonly targetAccountId = signal('');
  protected readonly submitting = signal(false);

  protected readonly otherAccounts = computed(() =>
    this.accounts().filter((a) => a.accountId !== this.account().accountId),
  );

  protected readonly parsedAmount = computed<number | null>(() => {
    const value = Number(this.amount());
    if (!Number.isFinite(value) || value <= 0) return null;
    return Math.round(value * 100) / 100;
  });

  protected readonly amountInvalid = computed(
    () => this.amount() !== '' && this.parsedAmount() === null,
  );

  protected readonly canSubmit = computed(
    () =>
      this.parsedAmount() !== null &&
      (this.mode() !== 'transfer' || this.targetAccountId() !== ''),
  );

  protected setMode(mode: TransactionMode): void {
    this.mode.set(mode);
    this.store.clearWriteError();
  }

  protected onAmountInput(event: Event): void {
    this.amount.set((event.target as HTMLInputElement).value);
  }

  protected onTargetChange(event: Event): void {
    this.targetAccountId.set((event.target as HTMLSelectElement).value);
  }

  protected async submitForm(event: Event): Promise<void> {
    event.preventDefault();
    const amount = this.parsedAmount();
    if (amount === null || this.submitting()) return;
    if (this.mode() === 'transfer' && this.targetAccountId() === '') return;

    const accountId = this.account().accountId;
    this.submitting.set(true);
    try {
      const ok =
        this.mode() === 'deposit'
          ? await this.store.deposit(accountId, amount)
          : this.mode() === 'withdraw'
            ? await this.store.withdraw(accountId, amount)
            : await this.store.transfer(accountId, this.targetAccountId(), amount);
      if (ok) this.amount.set('');
    } finally {
      this.submitting.set(false);
    }
  }
}
