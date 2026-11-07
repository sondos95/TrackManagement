import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService, demoTokenUrl } from '../services/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (request, next) => {
  const base = inject(DOCUMENT).baseURI;
  const api = new URL(environment.apiBaseUrl, base);
  const url = new URL(request.url, base);
  const path = api.pathname.replace(/\/$/, '');

  if (url.origin !== api.origin ||
      (url.pathname !== path && !url.pathname.startsWith(path + '/')) ||
      url.pathname === new URL(demoTokenUrl, base).pathname) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const session = auth.session;
  const authorizedRequest = session
    ? request.clone({ setHeaders: { Authorization: 'Bearer ' + session.accessToken } })
    : request;

  return next(authorizedRequest).pipe(
    tap({ error: (error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && auth.reject(session)) {
        void router.navigate(['/'], {
          queryParams: { returnUrl: router.url, expired: true },
          replaceUrl: true,
        });
      }
    } }),
  );
};
