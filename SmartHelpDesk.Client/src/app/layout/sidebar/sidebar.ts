import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Auth } from '../../core/services/auth';

interface NavigationItem {
  label: string;
  route: string;
  icon: string;
  roles?: string[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  // #region Fields

  readonly navigation: NavigationItem[] = [
    { label: 'Overview', route: '/dashboard', icon: '◫' },
    { label: 'All tickets', route: '/tickets', icon: '▤', roles: ['Administrator'] },
    { label: 'My tickets', route: '/my-tickets', icon: '▧', roles: ['Employee'] },
    { label: 'Assigned tickets', route: '/assigned-tickets', icon: '◷', roles: ['SupportAgent'] },
    {
      label: 'Create ticket',
      route: '/tickets/create',
      icon: '+',
      roles: ['Administrator', 'Employee'],
    },
    { label: 'Support agents', route: '/agents', icon: '♙', roles: ['Administrator'] },
    {
      label: 'AI assistant',
      route: '/ai-assistant',
      icon: '✧',
      roles: ['Administrator', 'SupportAgent', 'Employee'],
    },
    { label: 'Audit history', route: '/audit-history', icon: '≋', roles: ['Administrator'] },
    { label: 'My profile', route: '/profile', icon: '◎' },
  ];

  private readonly failedImageUrls = new Set<string>();

  // #endregion

  // #region Ctor

  constructor(readonly auth: Auth) {}

  // #endregion

  // #region Navigation

  get visibleNavigation(): NavigationItem[] {
    return this.navigation.filter(
      (item) => !item.roles || item.roles.some((role) => this.auth.hasRole(role)),
    );
  }

  get roleLabel(): string {
    if (this.auth.isAdministrator()) return 'Administrator';
    if (this.auth.isSupportAgent()) return 'Support Agent';
    if (this.auth.isEmployee()) return 'Employee';
    return 'User';
  }

  // #endregion

  // #region Profile Image

  getInitial(fullName: string | null | undefined): string {
    return fullName?.trim().charAt(0).toUpperCase() || 'U';
  }

  onImageError(event: Event): void {
    const image = event.target as HTMLImageElement;

    if (image.dataset['failed'] === 'true') {
      return;
    }

    image.dataset['failed'] = 'true';
    image.style.display = 'none';
  }

  // #endregion
}
