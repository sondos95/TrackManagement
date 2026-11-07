import { Component, input, output } from '@angular/core';
import { parseStatus, TrackStatus, trackStatuses } from '../../models/track.models';

@Component({
  standalone: false,
  selector: 'app-track-status-filter',
  templateUrl: './track-status-filter.component.html',
})
export class TrackStatusFilterComponent {
  readonly status = input.required<TrackStatus | null>();
  readonly statusChange = output<TrackStatus | null>();
  protected readonly statuses = trackStatuses;

  protected selectStatus(value: string): void {
    this.statusChange.emit(parseStatus(value));
  }
}
