import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Frames the figure as either the live read-model balance or a replayed
 * point-in-time balance, so it is visually obvious which one is on screen.
 */
@Component({
  selector: 'app-balance-display',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './balance-display.html',
  styleUrl: './balance-display.css',
})
export class BalanceDisplay {
  readonly balance = input<number | null>(null);
  readonly asOf = input<string | null>(null);
  readonly loading = input(false);
}
