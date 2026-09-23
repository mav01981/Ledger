import { TestBed } from '@angular/core/testing';

import { ReplayScrubber, clampToRange } from './replay-scrubber';

describe('ReplayScrubber', () => {
  const MIN = new Date('2026-01-01T00:00:00.000Z');
  const MAX = new Date('2026-06-01T00:00:00.000Z');

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ReplayScrubber] }).compileComponents();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function setup() {
    const fixture = TestBed.createComponent(ReplayScrubber);
    fixture.componentRef.setInput('min', MIN);
    fixture.componentRef.setInput('max', MAX);
    fixture.componentRef.setInput('value', MAX);
    await fixture.whenStable();
    const emissions: Date[] = [];
    const subscription = fixture.componentInstance.valueChange.subscribe((d) =>
      emissions.push(d),
    );
    const slider = fixture.nativeElement.querySelector('input[type="range"]') as HTMLInputElement;
    return { fixture, emissions, subscription, slider };
  }

  it('clampToRange keeps positions inside the bounds', () => {
    expect(clampToRange(150, 100, 200)).toBe(150);
    expect(clampToRange(50, 100, 200)).toBe(100);
    expect(clampToRange(250, 100, 200)).toBe(200);
  });

  it('emits once after the 250ms quiet period, with the latest position', async () => {
    const { emissions, subscription, slider } = await setup();
    vi.useFakeTimers();

    const mid = MIN.getTime() + (MAX.getTime() - MIN.getTime()) / 2;
    slider.value = String(mid);
    slider.dispatchEvent(new Event('input'));
    slider.value = String(mid + 1000);
    slider.dispatchEvent(new Event('input'));

    expect(emissions).toHaveLength(0);
    vi.advanceTimersByTime(ReplayScrubber.DEBOUNCE_MS - 1);
    expect(emissions).toHaveLength(0);
    vi.advanceTimersByTime(1);

    expect(emissions).toHaveLength(1);
    expect(emissions[0].getTime()).toBe(mid + 1000);
    subscription.unsubscribe();
  });

  it('clamps an out-of-range drag into [min, max] before emitting', async () => {
    const { emissions, subscription, slider } = await setup();
    vi.useFakeTimers();

    slider.value = String(MAX.getTime() + 60_000);
    slider.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(ReplayScrubber.DEBOUNCE_MS);

    expect(emissions).toHaveLength(1);
    const emitted = emissions[0].getTime();
    expect(emitted).toBeGreaterThanOrEqual(MIN.getTime());
    expect(emitted).toBeLessThanOrEqual(MAX.getTime());
    subscription.unsubscribe();
  });

  it('does not emit a pending change after destroy', async () => {
    const { fixture, emissions, slider } = await setup();
    vi.useFakeTimers();

    slider.value = String(MIN.getTime());
    slider.dispatchEvent(new Event('input'));
    fixture.destroy();
    vi.advanceTimersByTime(ReplayScrubber.DEBOUNCE_MS * 2);

    expect(emissions).toHaveLength(0);
  });
});
