import { TestBed } from '@angular/core/testing';

import { ConsistencyGapPanel } from './consistency-gap-panel';
import { Account } from '../../../core/ledger.model';

describe('ConsistencyGapPanel', () => {
  const account = (lastEventVersion: number): Account => ({
    accountId: 'acc-1',
    accountType: 'Standard',
    balance: 100,
    status: 'Open',
    openedAt: '2026-01-01T00:00:00.000Z',
    lastEventVersion,
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ConsistencyGapPanel] }).compileComponents();
  });

  function panelOf(nativeElement: HTMLElement): HTMLElement | null {
    return nativeElement.querySelector('[role="status"]');
  }

  it('is hidden when there is no pending write', async () => {
    const fixture = TestBed.createComponent(ConsistencyGapPanel);
    fixture.componentRef.setInput('pending', null);
    fixture.componentRef.setInput('current', account(3));
    await fixture.whenStable();

    expect(panelOf(fixture.nativeElement)).toBeNull();
  });

  it('is visible while the read model is behind the accepted write', async () => {
    const fixture = TestBed.createComponent(ConsistencyGapPanel);
    fixture.componentRef.setInput('pending', { aggregateId: 'acc-1', newVersion: 5 });
    fixture.componentRef.setInput('current', account(4));
    await fixture.whenStable();

    const panel = panelOf(fixture.nativeElement);
    expect(panel).not.toBeNull();
    expect(panel?.textContent).toContain('5');
    expect(panel?.textContent).toContain('4');
  });

  it('is visible right after a write when no read model has arrived yet', async () => {
    const fixture = TestBed.createComponent(ConsistencyGapPanel);
    fixture.componentRef.setInput('pending', { aggregateId: 'acc-1', newVersion: 1 });
    fixture.componentRef.setInput('current', null);
    await fixture.whenStable();

    expect(panelOf(fixture.nativeElement)).not.toBeNull();
  });

  it('hides once the read model has caught up to the write', async () => {
    const fixture = TestBed.createComponent(ConsistencyGapPanel);
    fixture.componentRef.setInput('pending', { aggregateId: 'acc-1', newVersion: 5 });
    fixture.componentRef.setInput('current', account(5));
    await fixture.whenStable();

    expect(panelOf(fixture.nativeElement)).toBeNull();
  });
});
