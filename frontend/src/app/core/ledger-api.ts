import { HttpErrorResponse, HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, firstValueFrom, of, throwError } from 'rxjs';

import {
  Account,
  AccountType,
  BalanceAtResponse,
  CommandResult,
  TransferRequest,
  TransactionHistory,
} from './ledger.model';

/**
 * Thin wrapper over the Ledger REST API — one method per endpoint.
 * Every write generates its own idempotency key client-side, which doubles
 * as a demonstration of the idempotency pattern the backend enforces.
 */
@Injectable({ providedIn: 'root' })
export class LedgerApiService {
  private readonly http = inject(HttpClient);

  getAccounts(): Promise<Account[]> {
    return firstValueFrom(this.http.get<Account[]>('/api/accounts'));
  }

  getBalance(accountId: string): Promise<Account> {
    return firstValueFrom(this.http.get<Account>(`/api/accounts/${accountId}`));
  }

  getBalanceAt(accountId: string, asOf: Date): Promise<BalanceAtResponse> {
    const params = new HttpParams().set('asOf', asOf.toISOString());
    return firstValueFrom(
      this.http.get<BalanceAtResponse>(`/api/accounts/${accountId}/balance-at`, { params }),
    );
  }

  /** 404 means the read model has not projected the account yet — resolve to null. */
  getHistory(accountId: string): Promise<TransactionHistory | null> {
    return firstValueFrom(
      this.http.get<TransactionHistory>(`/api/accounts/${accountId}/history`).pipe(
        catchError((err: unknown) =>
          err instanceof HttpErrorResponse && err.status === 404
            ? of(null)
            : throwError(() => err),
        ),
      ),
    );
  }

  openAccount(accountType: AccountType): Promise<CommandResult> {
    return firstValueFrom(
      this.http.post<CommandResult>('/api/accounts', {
        accountId: crypto.randomUUID(),
        accountType,
        idempotencyKey: crypto.randomUUID(),
      }),
    );
  }

  deposit(accountId: string, amount: number): Promise<CommandResult> {
    return firstValueFrom(
      this.http.post<CommandResult>(`/api/accounts/${accountId}/deposit`, {
        amount,
        idempotencyKey: crypto.randomUUID(),
      }),
    );
  }

  withdraw(accountId: string, amount: number): Promise<CommandResult> {
    return firstValueFrom(
      this.http.post<CommandResult>(`/api/accounts/${accountId}/withdraw`, {
        amount,
        idempotencyKey: crypto.randomUUID(),
      }),
    );
  }

  transfer(request: TransferRequest): Promise<CommandResult> {
    return firstValueFrom(
      this.http.post<CommandResult>('/api/accounts/transfer', request),
    );
  }
}
