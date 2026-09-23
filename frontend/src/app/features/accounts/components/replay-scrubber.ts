import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, input, output } from '@angular/core';

/** Clamp an epoch-millisecond position into the scrubber's [min, max] bounds. */
export function clampToRange(valueMs: number, minMs: number, maxMs: number): number {
  return Math.min(Math.max(valueMs, minMs), maxMs);
}

/**
 * Dumb timeline slider: knows dates, not the API. It debounces internally
 * (spec: 250ms quiet period) so the parent fetches once per gesture, and it
 * clamps every emission to [min, max] so an out-of-range replay is unreachable.
 */
@Component({
  selector: 'app-replay-scrubber',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  templateUrl: './replay-scrubber.html',
  styleUrl: './replay-scrubber.css',
})
export class ReplayScrubber implements OnDestroy {
  static readonly DEBOUNCE_MS = 250;

  readonly min = input.required<Date>();
  readonly max = input.required<Date>();
  readonly value = input.required<Date>();
  readonly valueChange = output<Date>();

  private debounceHandle: ReturnType<typeof setTimeout> | null = null;

  protected onSliderInput(event: Event): void {
    const raw = Number((event.target as HTMLInputElement).value);
    const clampedMs = clampToRange(raw, this.min().getTime(), this.max().getTime());
    if (this.debounceHandle !== null) clearTimeout(this.debounceHandle);
    this.debounceHandle = setTimeout(() => {
      this.debounceHandle = null;
      this.valueChange.emit(new Date(clampedMs));
    }, ReplayScrubber.DEBOUNCE_MS);
  }

  ngOnDestroy(): void {
    if (this.debounceHandle !== null) clearTimeout(this.debounceHandle);
  }
}
