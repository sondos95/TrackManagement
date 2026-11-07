import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { distinctUntilChanged, map, Subject } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { loadTrackPage } from '../../services/load-track-page';

@Component({
  standalone: false,
  templateUrl: './track-detail-page.component.html',
})
export class TrackDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  readonly retry = new Subject<void>();
  readonly state$ = loadTrackPage(
    this.route.paramMap.pipe(map(params => params.get('id')!), distinctUntilChanged()),
    this.retry,
    id => this.api.detail(id),
  );
}
