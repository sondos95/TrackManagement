import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';

interface DemoSession {
  readonly accessToken: string;
}

export const demoTokenUrl = environment.apiBaseUrl.replace(/\/$/, '') + '/auth/demo-token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private currentSession: DemoSession | null = null;

  get session(): DemoSession | null {
    return this.currentSession;
  }

  startDemo() {
    return this.http.post<DemoSession>(demoTokenUrl, null).pipe(
      tap(session => { this.currentSession = session; }),
    );
  }

  reject(session: DemoSession | null): boolean {
    if (this.currentSession !== session) return false;
    this.currentSession = null;
    return true;
  }
}
