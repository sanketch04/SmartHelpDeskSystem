
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  // #region Fields

  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  loading = false;
  errorMessage = '';
  passwordVisible = false;

  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  // #endregion

  // #region Login

  submit(): void {
    this.errorMessage = '';

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    if (this.loading) {
      return;
    }

    this.loading = true;

    this.auth.login(this.loginForm.getRawValue())
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
          const requestedUrl =
            this.route.snapshot.queryParamMap.get('returnUrl');

          // Only allow local application paths.
          const safeUrl =
            requestedUrl?.startsWith('/') &&
            !requestedUrl.startsWith('//') &&
            !requestedUrl.startsWith('/login')
              ? requestedUrl
              : '/dashboard';

          void this.router.navigateByUrl(safeUrl);
        },
        error: (error: unknown) => {
          const response = error as {
            error?: { message?: string; errors?: string[] };
          };

          this.errorMessage =
            response.error?.message ??
            response.error?.errors?.join(', ') ??
            'Unable to sign in. Please check your credentials and try again.';
        }
      });
  }

  togglePasswordVisibility(): void {
    this.passwordVisible = !this.passwordVisible;
  }

  // #endregion
}
