import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { Account, CommandResult } from '../../../core/ledger.model';

/**
 * Visible only while the write side is ahead of the read model — makes the
 * eventual-consistency window an explained, visible thing instead of a race.
 */
@Component({
  selector: 'app-consistency-gap-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './consistency-gap-panel.html',
  styleUrl: './consistency-gap-panel.css',
})
export class ConsistencyGapPanel {
  readonly pending = input<CommandResult | null>(null);
  readonly current = input<Account | null>(null);

  readonly visible = computed(() => {
    const pending = this.pending();
    if (!pending) return false;
    const current = this.current();
    return !current || current.lastEventVersion < pending.newVersion;
  });
}
