import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const sessionGuard: CanActivateFn = (_, state) =>
  inject(AuthService).session !== null ||
  inject(Router).createUrlTree(['/'], { queryParams: { returnUrl: state.url } });
