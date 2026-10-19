import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { Auth } from '../../core/services/auth';
import { TicketService } from '../../core/services/ticket';
import { Ticket } from '../../models/tickets/ticket.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  tickets: Ticket[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(readonly auth: Auth, private readonly ticketsApi: TicketService, private readonly cdr: ChangeDetectorRef) {}

  get roleTitle(): string {
    if (this.auth.isAdministrator()) return 'Administrator overview';
    if (this.auth.isSupportAgent()) return 'Support workspace';
    return 'Employee workspace';
  }
  get greeting(): string { return this.auth.getCurrentUser()?.fullName?.split(' ')[0] || 'there'; }
  get openCount(): number { return this.tickets.filter(t => t.status === 'Open').length; }
  get progressCount(): number { return this.tickets.filter(t => t.status === 'In Progress' || t.status === 'Pending').length; }
  get resolvedCount(): number { return this.tickets.filter(t => t.status === 'Resolved' || t.status === 'Closed').length; }
  get urgentCount(): number { return this.tickets.filter(t => t.priority === 'High' || t.priority === 'Critical').length; }

  ngOnInit(): void { this.loadOverview(); }

  loadOverview(): void {
    this.isLoading = true;
    this.errorMessage = '';
    const request = this.auth.isAdministrator()
      ? this.ticketsApi.getAll()
      : this.auth.isSupportAgent()
        ? this.ticketsApi.getAssignedTickets()
        : this.ticketsApi.getMyTickets();
    request.pipe(timeout(15000), finalize(() => { this.isLoading = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: items => { this.tickets = Array.isArray(items) ? items : []; this.cdr.markForCheck(); },
        error: error => {
          const e = error as { status?: number; name?: string };
          this.errorMessage = e.name === 'TimeoutError'
            ? 'The overview request timed out. Please refresh.'
            : e.status === 403
              ? 'Your account does not have permission to load this overview.'
              : e.status === 401
                ? 'Your session has expired. Please sign in again.'
                : 'Ticket statistics are temporarily unavailable.';
          this.cdr.markForCheck();
        }
      });
  }
}
