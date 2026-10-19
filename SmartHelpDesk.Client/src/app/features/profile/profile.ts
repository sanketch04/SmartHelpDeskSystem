import { Component } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';
import { Auth } from '../../core/services/auth';
import { API_BASE_URL } from '../../core/api-config';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile {
  selectedFile: File | null = null;
  isUploading = false;
  errorMessage = '';
  successMessage = '';
  private readonly baseUrl = `${API_BASE_URL}/ProfileImages`;

  constructor(readonly auth: Auth, private readonly http: HttpClient) {}

  get profileImageUrl(): string | null {
    const value = this.auth.getCurrentUser()?.profileImageUrl;
    if (!value) return null;
    if (/^https?:\/\//i.test(value)) return value;
    const apiOrigin = API_BASE_URL.replace(/\/api\/?$/i, '');
    return `${apiOrigin}/${value.replace(/^\/+/, '')}`;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.errorMessage = '';
    this.successMessage = '';
    const file = input.files?.[0] ?? null;
    if (!file) { this.selectedFile = null; return; }
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      this.errorMessage = 'Choose a JPEG, PNG, or WebP image.';
      input.value = '';
      this.selectedFile = null;
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.errorMessage = 'The maximum image size is 5 MB.';
      input.value = '';
      this.selectedFile = null;
      return;
    }
    this.selectedFile = file;
  }

  uploadImage(): void {
    if (!this.selectedFile || this.isUploading) return;
    const form = new FormData();
    form.append('Image', this.selectedFile, this.selectedFile.name);
    this.isUploading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.http.put<{ message: string; profileImageUrl: string }>(`${this.baseUrl}/me`, form)
      .pipe(finalize(() => this.isUploading = false))
      .subscribe({
        next: result => {
          const session = this.auth.getSession();
          if (session) this.auth.saveSession({ ...session, profileImageUrl: result.profileImageUrl });
          this.successMessage = result.message || 'Profile image updated successfully.';
          this.selectedFile = null;
        },
        error: error => {
          const e = error as { status?: number; error?: { message?: string } };
          this.errorMessage = e.error?.message || (e.status === 401 ? 'Your session has expired. Please sign in again.' : 'Unable to update the profile image.');
        }
      });
  }
}
