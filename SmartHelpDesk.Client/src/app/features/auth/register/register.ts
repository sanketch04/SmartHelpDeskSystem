
import { Component, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  // #region Fields

  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  loading = false;
  errorMessage = '';
  successMessage = '';
  selectedImage: File | null = null;
  imagePreview: string | null = null;

  readonly registerForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  });

  // #endregion

  // #region Image Selection

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    this.errorMessage = '';

    if (!file) {
      return;
    }

    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];

    if (!allowedTypes.includes(file.type)) {
      this.errorMessage = 'Choose a JPEG, PNG, or WebP image.';
      input.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.errorMessage = 'The profile image must be 5 MB or smaller.';
      input.value = '';
      return;
    }

    if (this.imagePreview) {
      URL.revokeObjectURL(this.imagePreview);
    }

    this.selectedImage = file;
    this.imagePreview = URL.createObjectURL(file);
  }

  removeImage(input: HTMLInputElement): void {
    this.selectedImage = null;
    input.value = '';

    if (this.imagePreview) {
      URL.revokeObjectURL(this.imagePreview);
      this.imagePreview = null;
    }
  }

  // #endregion

  // #region Registration

  submit(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const form = this.registerForm.getRawValue();

    if (form.password !== form.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    if (this.loading) {
      return;
    }

    this.loading = true;

    this.auth.register({
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      password: form.password,
      profileImage: this.selectedImage
    })
    .pipe(finalize(() => this.loading = false))
    .subscribe({
      next: response => {
        this.successMessage =
          response.message || 'Registration successful. You can now sign in.';

        this.registerForm.reset();
        this.removeImageFromForm();
      },
      error: (error: unknown) => {
        const response = error as {
          error?: {
            message?: string;
            errors?: string[];
          };
        };

        this.errorMessage =
          response.error?.message ??
          response.error?.errors?.join(', ') ??
          'Registration failed. Please review your details and try again.';
      }
    });
  }

  private removeImageFromForm(): void {
    this.selectedImage = null;

    if (this.imagePreview) {
      URL.revokeObjectURL(this.imagePreview);
      this.imagePreview = null;
    }
  }

  // #endregion
}
