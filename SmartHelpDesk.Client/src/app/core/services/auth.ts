import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../api-config';
import { AuthResponse, LoginRequest, RegisterRequest } from '../../models/auth/auth.model';

export type AppRole = 'Administrator' | 'SupportAgent' | 'Employee';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly sessionKey = 'smartHelpDeskAuth';
  private readonly loginUrl = `${API_BASE_URL}/Auth/login`;
  private readonly registerUrl = `${API_BASE_URL}/Auth/register`;
  private readonly refreshUrl = `${API_BASE_URL}/Auth/refresh`;
  private readonly logoutUrl = `${API_BASE_URL}/Auth/logout`;

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.loginUrl, request)
      .pipe(tap(response => this.saveSession(response)));
  }

  register(request: RegisterRequest): Observable<{ message: string }> {
    const formData = new FormData();
    formData.append('FullName', request.fullName.trim());
    formData.append('Email', request.email.trim());
    formData.append('Password', request.password);
    if (request.profileImage) {
      formData.append('ProfileImage', request.profileImage, request.profileImage.name);
    }
    return this.http.post<{ message: string }>(this.registerUrl, formData);
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) throw new Error('No refresh token is available.');
    return this.http.post<AuthResponse>(this.refreshUrl, { refreshToken })
      .pipe(tap(response => this.saveSession(response)));
  }

  saveSession(response: AuthResponse): void {
    if (!response.accessToken || !response.refreshToken) {
      throw new Error('The authentication response does not contain valid tokens.');
    }
    sessionStorage.setItem(this.sessionKey, JSON.stringify(response));
  }

  getSession(): AuthResponse | null {
    const stored = sessionStorage.getItem(this.sessionKey);
    if (!stored) return null;
    try { return JSON.parse(stored) as AuthResponse; }
    catch { this.clearSession(); return null; }
  }

  getAccessToken(): string | null { return this.getSession()?.accessToken ?? null; }
  getRefreshToken(): string | null { return this.getSession()?.refreshToken ?? null; }
  getCurrentUser(): AuthResponse | null { return this.getSession(); }

  isAuthenticated(): boolean {
    const session = this.getSession();
    if (!session?.accessToken || !session.expiresAtUtc) return false;
    const expiry = Date.parse(session.expiresAtUtc);
    return Number.isFinite(expiry) && expiry > Date.now();
  }

  // Roles are read from the signed access token for UI navigation only.
  // The API remains the authority and enforces every permission independently.
  getRoles(): string[] {
    const token = this.getAccessToken();
    if (!token) return [];
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return [];
      const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = decodeURIComponent(
        atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '='))
          .split('')
          .map(char => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
          .join('')
      );
      const payload = JSON.parse(decoded) as Record<string, unknown>;
      const roleClaim = Object.entries(payload).find(([key]) =>
        key === 'role' ||
        key === 'roles' ||
        key === 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
      )?.[1];
      if (Array.isArray(roleClaim)) return roleClaim.map(String);
      return typeof roleClaim === 'string' ? [roleClaim] : [];
    } catch {
      return [];
    }
  }

  hasRole(role: string): boolean { return this.getRoles().includes(role); }
  isAdministrator(): boolean { return this.hasRole('Administrator'); }
  isSupportAgent(): boolean { return this.hasRole('SupportAgent'); }
  isEmployee(): boolean { return this.hasRole('Employee'); }

  clearSession(): void { sessionStorage.removeItem(this.sessionKey); }

  logout(): Observable<void> {
    const token = this.getAccessToken();
    if (!token) {
      this.clearSession();
      return new Observable<void>(subscriber => { subscriber.next(); subscriber.complete(); });
    }
    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });
    return this.http.post<void>(this.logoutUrl, null, { headers }).pipe(
      tap({ next: () => this.clearSession(), error: () => this.clearSession() })
    );
  }
}
