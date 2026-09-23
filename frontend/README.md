# Ledger Dashboard — Angular Frontend

Angular 21 (standalone, zoneless, signals + `OnPush`) frontend for the CQRS + event-sourced
Ledger backend. See `../docs/plan.md` and `../docs/spec-frontEnd.md` for the full plan and spec.

The headline feature is the **replay scrubber**: drag the timeline and the balance display
reconstructs itself as of that instant by calling the backend's
`GET /api/accounts/{id}/balance-at?asOf=` — real server-side event replay, not a simulation.

## Prerequisites

- Node.js 22.22+ / 24.11+ (Angular 21 supported range)
- The backend running at `http://localhost:5001` (`docker compose up --build` from the repo root)

## Run

```bash
npm install
npm start          # ng serve with a dev proxy: /api → http://localhost:5001
```

Open `http://localhost:4200/`. The dev-server proxy (`proxy.conf.json`) forwards `/api` to the
backend, so no CORS configuration is required on either side.

## Test & build

```bash
npm test           # Vitest unit tests (add -- --no-watch for a single run: npx ng test --no-watch)
npm run test:e2e   # Playwright e2e — needs the backend running; starts (or reuses) ng serve itself
npx ng build       # production build — also the strict template type-check
```

Playwright config lives in `playwright.config.ts`; specs are in `e2e/`. The suite covers the
spec's happy path: open account → deposit → consistency-gap panel → scrubber replay, plus inline
surfacing of an API rejection. CI (`.github/workflows/ci.yml`, `frontend` job) runs build, unit
tests, and the Playwright suite against a fresh `docker compose up --build` backend.

## Notes on spec vs. backend reality

Data shapes in `src/app/core/ledger.model.ts` were confirmed against
`src/Ledger.Api/Controllers/AccountsController.cs` rather than assumed:

- Write endpoints return `{ aggregateId, newVersion }` (not `streamVersion`).
- `GET /api/accounts/{id}/history` exists and powers the event-stream view.
- Reads return `accountType`/`status` as enum *names* (`"Standard"`, `"Open"`);
  write requests send `accountType` as a number (`0`/`1`).
- Consistency-gap check: read model `lastEventVersion` starts at 0 on open and increments per
  projected event, matching the aggregate's 0-based stream version — safe to compare directly.

## Deviations from `docs/plan.md` / `docs/spec-frontEnd.md`

- **Angular 21, not 22** — CLI 22 requires Node ≥ 24.15; this machine runs Node 24.11.1 with no
  version manager available. Consequences: `@Injectable({ providedIn: 'root' })` instead of the
  experimental `@Service()`, and **explicit `ChangeDetectionStrategy.OnPush` on every component**
  (OnPush does not become the default until Angular 22).
- **Replay scrubber uses a native `<input type="range">`**, not an Angular CDK/Material slider —
  the CDK has no slider component, and the native input gives DOM-level clamping and built-in
  accessibility (keyboard, ARIA) with zero extra dependencies. The component keeps the spec's
  dumb, API-agnostic contract and 250 ms debounce exactly.
- **Dev-server proxy instead of CORS** — `proxy.conf.json` forwards `/api` → `http://localhost:5001`;
  the backend has no CORS configuration, so this avoids requiring one.

## End-to-end smoke test (verified 2026-09-23)

With `docker compose up --build -d` + `npm start`: shell serves at `/` and `/accounts` (200),
the proxy relays `/api/accounts`, and the full write → project → read cycle was exercised
(open → deposit → withdraw; read model caught up to `lastEventVersion` within the polling window;
`balance-at` and `history` return the shapes `ledger.model.ts` declares).
