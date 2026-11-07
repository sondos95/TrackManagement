import { Component, input } from '@angular/core';
import { TrackStatus, TrackSummary } from '../../models/track.models';

@Component({
  standalone: false,
  selector: 'app-track-table',
  templateUrl: './track-table.component.html',
})
export class TrackTableComponent {
  readonly tracks = input.required<readonly TrackSummary[]>();
  readonly status = input.required<TrackStatus | null>();
  readonly page = input.required<number>();
}
