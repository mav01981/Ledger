import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { TransactionEntry } from '../../../core/ledger.model';

@Component({
  selector: 'app-transaction-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './transaction-history.html',
  styleUrl: './transaction-history.css',
})
export class TransactionHistory {
  readonly transactions = input<TransactionEntry[] | null>(null);

  /** Running balance over time as an SVG polyline (x: 0-100, y: 0-30). */
  protected readonly sparklinePoints = computed(() => {
    const entries = this.transactions();
    if (!entries || entries.length < 2) return '';
    const times = entries.map((e) => new Date(e.timestamp).getTime());
    const balances = entries.map((e) => e.runningBalance);
    const minTime = Math.min(...times);
    const maxTime = Math.max(...times);
    const minBalance = Math.min(...balances);
    const maxBalance = Math.max(...balances);
    const timeSpan = maxTime - minTime || 1;
    const balanceSpan = maxBalance - minBalance || 1;
    return entries
      .map((_, index) => {
        const x = ((times[index] - minTime) / timeSpan) * 100;
        const y = 30 - ((balances[index] - minBalance) / balanceSpan) * 30;
        return `${x.toFixed(2)},${y.toFixed(2)}`;
      })
      .join(' ');
  });
}
