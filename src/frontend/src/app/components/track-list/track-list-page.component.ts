import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map, Subject } from 'rxjs';
import { parseStatus, TrackStatus } from '../../models/track.models';
import { ApiService } from '../../services/api.service';
import { loadTrackPage } from '../../services/load-track-page';

@Component({
  standalone: false,
  templateUrl: './track-list-page.component.html',
})
export class TrackListPageComponent {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  readonly retry = new Subject<void>();
  readonly pageSize = 5;
  readonly requestedPage$ = this.route.queryParamMap.pipe(
    map(params => Number(params.get('page'))),
    map(page => Number.isSafeInteger(page) && page > 0 ? page : 1),
    distinctUntilChanged(),
  );
  readonly status$ = this.route.queryParamMap.pipe(
    map(params => parseStatus(params.get('status'))),
    distinctUntilChanged(),
  );
  readonly state$ = loadTrackPage(this.status$, this.retry, status => this.api.list(status));

  pageCount(trackCount: number): number {
    return Math.ceil(trackCount / this.pageSize);
  }

  visiblePage(requestedPage: number, pageCount: number): number {
    return Math.min(requestedPage, pageCount);
  }

  setFilter(status: TrackStatus | null): void {
    void this.router.navigate(['/tracks'], { queryParams: { status } });
  }
}
