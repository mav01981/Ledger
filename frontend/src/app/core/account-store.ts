import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';

import { LedgerApiService } from './ledger-api';
import { Account, AccountType, BalanceAtResponse, CommandResult, TransactionEntry } from './ledger.model';

const POLL_INTERVAL_MS = 3000;
const MAX_CONSECUTIVE_FAILURES = 3;

/**
 * Single source of truth for the dashboard. Components read signals via
 * computed(); this store is the only caller of LedgerApiService.
 */
@Injectable({ providedIn: 'root' })
export class AccountStore {
  private readonly api = inject(LedgerApiService);
  private readonly doc = inject(DOCUMENT);

  readonly accounts = signal<Account[]>([]);
  readonly accountsLoading = signal(false);
  readonly accountsError = signal<string | null>(null);

  readonly selectedAccount = signal<Account | null>(null);
  readonly accountNotFound = signal(false);
  readonly history = signal<TransactionEntry[] | null>(null);

  /** Result of the current asOf query — non-null means "replay mode". */
  readonly scrubberBalance = signal<BalanceAtResponse | null>(null);
  readonly scrubberLoading = signal(false);
  readonly scrubberMax = signal(new Date());
  readonly scrubError = signal<string | null>(null);

  /** Drives the consistency-gap panel until polling catches up. */
  readonly pendingWrite = signal<CommandResult | null>(null);
  readonly writeError = signal<string | null>(null);

  readonly connectionLost = signal(false);

  readonly isScrubbing = computed(() => this.scrubberBalance() !== null);

  /** Session-lifetime cache keyed by `accountId|asOf-ISO` — re-scrubbing is instant. */
  private readonly balanceCache = new Map<string, BalanceAtResponse>();
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private pollingAccountId: string | null = null;
  private consecutiveFailures = 0;

  /* ---------------- Accounts ---------------- */

  async loadAccounts(): Promise<void> {
    this.accountsLoading.set(true);
    this.accountsError.set(null);
    try {
      this.accounts.set(await this.api.getAccounts());
    } catch (err) {
      this.accountsError.set(extractError(err));
    } finally {
      this.accountsLoading.set(false);
    }
  }

  async openAccount(accountType: AccountType): Promise<boolean> {
    this.writeError.set(null);
    try {
      const result = await this.api.openAccount(accountType);
      await this.loadAccounts();
      // The write returns before the outbox projects the account — keep
      // refreshing until it shows up, mirroring the detail page's catch-up loop.
      await this.waitForProjection(result.aggregateId);
      return true;
    } catch (err) {
      this.writeError.set(extractError(err));
      return false;
    }
  }

  /**
   * Bounded retry: refresh the list until `accountId` has been projected
   * (~6s max). Best-effort — if the projection is slower than that, the next
   * manual refresh picks it up.
   */
  private async waitForProjection(accountId: string, attempts = 12, delayMs = 500): Promise<void> {
    for (let i = 0; i < attempts; i++) {
      if (this.accounts().some((a) => a.accountId === accountId)) return;
      await new Promise((resolve) => setTimeout(resolve, delayMs));
      try {
        this.accounts.set(await this.api.getAccounts());
      } catch {
        // Transient refresh failure — the next attempt retries.
      }
    }
  }

  clearWriteError(): void {
    this.writeError.set(null);
  }

  /* ---------------- Selection ---------------- */

  async selectAccount(accountId: string): Promise<void> {
    this.selectedAccount.set(null);
    this.accountNotFound.set(false);
    this.history.set(null);
    this.scrubberBalance.set(null);
    this.scrubberLoading.set(false);
    this.scrubError.set(null);
    this.pendingWrite.set(null);
    this.writeError.set(null);
    this.scrubberMax.set(new Date());

    try {
      this.selectedAccount.set(await this.api.getBalance(accountId));
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        this.accountNotFound.set(true);
      } else {
        this.accountsError.set(extractError(err));
      }
    }

    try {
      const history = await this.api.getHistory(accountId);
      this.history.set(history?.transactions ?? []);
    } catch {
      this.history.set([]);
    }
  }

  /* ---------------- Polling ("live-ish") ---------------- */

  startPolling(accountId: string): void {
    this.stopPolling();
    this.pollingAccountId = accountId;
    this.consecutiveFailures = 0;
    this.connectionLost.set(false);
    void this.pollOnce();
    this.pollTimer = setInterval(() => void this.pollOnce(), POLL_INTERVAL_MS);
  }

  stopPolling(): void {
    this.clearTimer();
    this.pollingAccountId = null;
  }

  retryConnection(): void {
    const accountId = this.pollingAccountId ?? this.selectedAccount()?.accountId;
    this.consecutiveFailures = 0;
    this.connectionLost.set(false);
    if (accountId) {
      this.startPolling(accountId);
    }
  }

  private async pollOnce(): Promise<void> {
    const accountId = this.pollingAccountId;
    if (!accountId) return;
    // Backgrounded tabs don't burn requests — the next tick resumes.
    if (this.doc.visibilityState !== 'visible') return;

    try {
      const account = await this.api.getBalance(accountId);
      if (this.pollingAccountId !== accountId) return;
      this.consecutiveFailures = 0;
      this.accountNotFound.set(false);
      this.selectedAccount.set(account);
      this.accounts.update((list) => {
        const index = list.findIndex((a) => a.accountId === accountId);
        if (index === -1) return [...list, account];
        const next = [...list];
        next[index] = account;
        return next;
      });
      // Each successful tick extends the scrubber's upper bound ("now").
      this.scrubberMax.set(new Date());
      this.resolvePendingWrite(account);
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        // Not projected yet (e.g. account just opened) — keep polling quietly.
        this.accountNotFound.set(true);
        return;
      }
      this.consecutiveFailures += 1;
      if (this.consecutiveFailures >= MAX_CONSECUTIVE_FAILURES) {
        this.clearTimer();
        this.connectionLost.set(true);
      }
    }
  }

  private resolvePendingWrite(account: Account): void {
    const pending = this.pendingWrite();
    if (!pending || pending.aggregateId !== account.accountId) return;
    if (account.lastEventVersion >= pending.newVersion) {
      this.pendingWrite.set(null);
      void this.refreshHistory(account.accountId);
    }
  }

  private async refreshHistory(accountId: string): Promise<void> {
    try {
      const history = await this.api.getHistory(accountId);
      if (this.selectedAccount()?.accountId === accountId) {
        this.history.set(history?.transactions ?? []);
      }
    } catch {
      // History refresh is best-effort; the next catch-up will retry.
    }
  }

  private clearTimer(): void {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  /* ---------------- Writes ---------------- */

  async deposit(accountId: string, amount: number): Promise<boolean> {
    return this.executeWrite(() => this.api.deposit(accountId, amount));
  }

  async withdraw(accountId: string, amount: number): Promise<boolean> {
    return this.executeWrite(() => this.api.withdraw(accountId, amount));
  }

  async transfer(fromAccountId: string, toAccountId: string, amount: number): Promise<boolean> {
    return this.executeWrite(() =>
      this.api.transfer({
        fromAccountId,
        toAccountId,
        amount,
        idempotencyKey: crypto.randomUUID(),
      }),
    );
  }

  private async executeWrite(write: () => Promise<CommandResult>): Promise<boolean> {
    this.writeError.set(null);
    try {
      const result = await write();
      this.pendingWrite.set(result);
      // Kick the read model immediately instead of waiting for the next tick.
      void this.pollOnce();
      return true;
    } catch (err) {
      this.writeError.set(extractError(err));
      return false;
    }
  }

  /* ---------------- Replay scrubber ---------------- */

  async scrubTo(asOf: Date): Promise<void> {
    const accountId = this.selectedAccount()?.accountId;
    if (!accountId) return;

    const key = `${accountId}|${asOf.toISOString()}`;
    const cached = this.balanceCache.get(key);
    if (cached) {
      this.scrubberBalance.set(cached);
      this.scrubError.set(null);
      return;
    }

    this.scrubberLoading.set(true);
    try {
      const response = await this.api.getBalanceAt(accountId, asOf);
      this.balanceCache.set(key, response);
      if (this.selectedAccount()?.accountId === accountId) {
        this.scrubberBalance.set(response);
        this.scrubError.set(null);
      }
    } catch {
      if (this.selectedAccount()?.accountId === accountId) {
        this.scrubError.set('Replay query failed — showing the last resolved point in time.');
      }
    } finally {
      this.scrubberLoading.set(false);
    }
  }

  clearScrub(): void {
    this.scrubberBalance.set(null);
    this.scrubError.set(null);
  }
}

function extractError(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as { error?: unknown } | null;
    if (typeof body?.error === 'string') return body.error;
    if (err.status === 0) return 'Cannot reach the API. Is it running at localhost:5001?';
    return `Request failed with status ${err.status}.`;
  }
  return 'Unexpected error.';
}
