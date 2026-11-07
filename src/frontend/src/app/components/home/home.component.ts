import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: false,
  templateUrl: './home.component.html',
})
export class HomeComponent {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly loading = signal(false);
  readonly failed = signal(false);

  get expired(): boolean {
    return this.route.snapshot.queryParamMap.get('expired') === 'true';
  }

  startDemo(): void {
    if (this.loading()) return;

    this.loading.set(true);
    this.failed.set(false);
    this.auth.startDemo().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const destination = returnUrl && /^\/tracks(?:\/[^/?#\\]+)?(?:[?#]|$)/.test(returnUrl)
          ? returnUrl : '/tracks';
        void this.router.navigateByUrl(destination, { replaceUrl: true });
      },
      error: () => this.failed.set(true),
    });
  }
}
