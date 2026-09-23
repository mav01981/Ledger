import { APIRequestContext, expect, test } from '@playwright/test';

/**
 * E2E happy path from docs/spec-frontEnd.md:
 * open account → deposit → consistency-gap panel → scrubber replay,
 * plus inline surfacing of an API rejection.
 *
 * Tests run serially (workers: 1) against the live backend so list counts
 * and balances stay deterministic.
 */

/** Open an account through the API and wait until the read model projects it. */
async function seedAccount(request: APIRequestContext): Promise<string> {
  const accountId = crypto.randomUUID();
  const res = await request.post('/api/accounts', {
    data: { accountId, accountType: 0, idempotencyKey: crypto.randomUUID() },
  });
  expect(res.ok(), 'seed: POST /api/accounts should succeed').toBe(true);
  await expect
    .poll(async () => (await request.get(`/api/accounts/${accountId}`)).status(), {
      timeout: 15_000,
      message: 'read model should project the seeded account',
    })
    .toBe(200);
  return accountId;
}

async function seedDeposit(
  request: APIRequestContext,
  accountId: string,
  amount: number,
): Promise<void> {
  const res = await request.post(`/api/accounts/${accountId}/deposit`, {
    data: { amount, idempotencyKey: crypto.randomUUID() },
  });
  expect(res.ok(), 'seed: POST deposit should succeed').toBe(true);
}

test('open an account from the list and see it appear once projected', async ({ page }) => {
  await page.goto('/accounts');
  // Wait for the initial load (cards or empty state) before counting.
  await expect(page.locator('.empty-state, ul[aria-label="Accounts"]')).toBeVisible();

  const cards = page.locator('ul[aria-label="Accounts"] li');
  const before = await cards.count();

  await page.getByRole('button', { name: 'Open account' }).click();

  await expect(cards).toHaveCount(before + 1, { timeout: 15_000 });
});

test('deposit resolves through the consistency-gap panel into balance and history', async ({
  page,
  request,
}) => {
  const accountId = await seedAccount(request);
  await seedDeposit(request, accountId, 40);

  await page.goto(`/accounts/${accountId}`);
  const balance = page.locator('.balance-amount');
  await expect(balance).toContainText('40.00', { timeout: 15_000 });

  await page.locator('#tx-amount').fill('25.50');
  await page.getByRole('button', { name: 'Record deposit' }).click();

  // The gap panel explains the lag between write version and read version.
  const gapPanel = page.locator('.gap-panel');
  await expect(gapPanel).toBeVisible({ timeout: 5_000 });
  await expect(gapPanel).toContainText('stream version');

  // Polling catches up: balance updates, panel clears, history gains the row.
  await expect(balance).toContainText('65.50', { timeout: 15_000 });
  await expect(gapPanel).toBeHidden({ timeout: 15_000 });
  await expect(
    page.locator('.tx-list .tx-row').filter({ hasText: '25.50' }),
  ).toBeVisible({ timeout: 10_000 });
});

test('replay scrubber rebuilds the balance as of a past moment and returns to live', async ({
  page,
  request,
}) => {
  const accountId = await seedAccount(request);
  await seedDeposit(request, accountId, 40);

  await page.goto(`/accounts/${accountId}`);
  const balance = page.locator('.balance-amount');
  await expect(balance).toContainText('40.00', { timeout: 15_000 });

  // Jump to the first event: the balance must be rebuilt server-side (0.00).
  const slider = page.getByLabel(
    'Replay timeline: drag to rebuild the balance as of a past moment',
  );
  await slider.press('Home');
  await expect(page.locator('.balance-label')).toContainText('Replayed balance', {
    timeout: 5_000,
  });
  await expect(page.locator('.balance')).toHaveClass(/replayed/);
  await expect(balance).toContainText('0.00', { timeout: 5_000 });

  await page.getByRole('button', { name: 'Jump back to now' }).click();
  await expect(page.locator('.balance-label')).toContainText('Current balance');
  await expect(balance).toContainText('40.00');
});

test('an API rejection is surfaced inline on the transaction form', async ({ page, request }) => {
  const accountId = await seedAccount(request);
  await seedDeposit(request, accountId, 10);

  await page.goto(`/accounts/${accountId}`);
  await expect(page.locator('.balance-amount')).toContainText('10.00', { timeout: 15_000 });

  // Default mode is deposit — switch to withdraw and overdraw.
  await page.locator('.mode-toggle label').filter({ hasText: 'withdraw' }).click();
  await page.locator('#tx-amount').fill('99999');
  await page.getByRole('button', { name: 'Record withdraw' }).click();

  await expect(page.locator('app-transaction-form .alert.error')).toContainText(
    'Insufficient funds.',
    { timeout: 10_000 },
  );
});
