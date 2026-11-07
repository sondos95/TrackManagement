import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { TrackDetails, TrackStatus, TrackSummary } from '../models/track.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly tracksUrl = environment.apiBaseUrl.replace(/\/$/, '') + '/tracks';

  list(status: TrackStatus | null) {
    return this.http.get<TrackSummary[]>(this.tracksUrl, { params: status ? { status } : {} });
  }

  detail(id: string) {
    return this.http.get<TrackDetails>(this.tracksUrl + '/' + encodeURIComponent(id));
  }
}
