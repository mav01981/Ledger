import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Account } from '../../../core/ledger.model';

@Component({
  selector: 'app-account-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './account-card.html',
  styleUrl: './account-card.css',
})
export class AccountCard {
  readonly account = input.required<Account>();
}
