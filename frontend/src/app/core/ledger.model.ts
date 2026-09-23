/**
 * Wire shapes confirmed against Ledger.Api Controllers/AccountsController.cs.
 * ASP.NET serializes with camelCase naming, so these match the JSON exactly.
 */

export interface Account {
  accountId: string;
  /** Read model stores the enum name: "Standard" | "Overdraft". */
  accountType: string;
  balance: number;
  status: string;
  openedAt: string;
  lastEventVersion: number;
}

export interface BalanceAtResponse {
  accountId: string;
  asOf: string;
  balance: number;
}

/** Success body of every write endpoint: new { aggregateId, newVersion }. */
export interface CommandResult {
  aggregateId: string;
  newVersion: number;
}

export interface TransactionEntry {
  transactionId: string;
  timestamp: string;
  amount: number;
  direction: string;
  runningBalance: number;
}

export interface TransactionHistory {
  accountId: string;
  transactions: TransactionEntry[];
}

export interface TransferRequest {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  idempotencyKey: string;
}

/** Write requests send the enum by number (0 = Standard, 1 = Overdraft). */
export enum AccountType {
  Standard = 0,
  Overdraft = 1,
}
