import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css'
})
export class Topbar {
  isLoggingOut = false;
  constructor(readonly auth: Auth, private readonly router: Router) {}

  logout(): void {
    if (this.isLoggingOut) return;
    this.isLoggingOut = true;
    this.auth.logout().subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout()
    });
  }

  private finishLogout(): void {
    this.auth.clearSession();
    this.isLoggingOut = false;
    void this.router.navigate(['/login']);
  }
}
