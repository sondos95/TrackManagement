import { HttpErrorResponse } from '@angular/common/http';
import { catchError, combineLatest, map, Observable, of, startWith, switchMap } from 'rxjs';

type LoadState<T> =
  | { kind: 'loading' }
  | { kind: 'ready'; data: T }
  | { kind: 'error'; status: number };

export function loadTrackPage<P, T>(
  parameters: Observable<P>,
  refresh: Observable<void>,
  request: (parameter: P) => Observable<T>,
): Observable<LoadState<T>> {
  return combineLatest([parameters, refresh.pipe(startWith(undefined))]).pipe(
    switchMap(([parameter]) => request(parameter).pipe(
      map((data): LoadState<T> => ({ kind: 'ready', data })),
      startWith({ kind: 'loading' } as const),
      catchError((error: HttpErrorResponse) => of({ kind: 'error', status: error.status } as const)),
    )),
  );
}
