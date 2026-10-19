import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, map, of, switchMap, timeout } from 'rxjs';
import { TicketService } from '../../core/services/ticket';
import { Ticket } from '../../models/tickets/ticket.model';
import { TicketHistory } from '../../models/tickets/ticket-history.model';

interface AuditRow extends TicketHistory { ticketTitle: string; }
@Component({
  selector: 'app-audit-history',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './audit-history.html',
  styleUrl: './audit-history.css'
})
export class AuditHistory implements OnInit {
  events: AuditRow[] = [];
  isLoading = false;
  errorMessage = '';
  searchTerm = '';
  constructor(private readonly tickets: TicketService, private readonly cdr: ChangeDetectorRef) {}
  ngOnInit(): void { this.load(); }

  get filteredEvents(): AuditRow[] {
    const q = this.searchTerm.trim().toLowerCase();
    return this.events.filter(e => !q || `${e.ticketTitle} ${e.description} ${e.action} ${e.performedByUserName}`.toLowerCase().includes(q));
  }

  load(): void {
    this.isLoading = true; this.errorMessage = ''; this.cdr.markForCheck();
    this.tickets.getAll().pipe(
      timeout(15000),
      switchMap(items => {
        if (!Array.isArray(items) || items.length === 0) return of([] as AuditRow[]);
        const requests = items.map(ticket => this.tickets.getAuditHistory(ticket.id).pipe(
          catchError(() => of([] as unknown[])),
          map(history => ({ ticket, history }))
        ));
        return forkJoin(requests).pipe(
          map(results => results.flatMap(result =>
            (Array.isArray(result.history) ? result.history as TicketHistory[] : [])
              .map(entry => ({ ...entry, ticketTitle: result.ticket.title }))
          ))
        );
      }),
      timeout(30000),
      finalize(() => { this.isLoading = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: rows => { this.events = rows.sort((a,b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)); this.cdr.markForCheck(); },
      error: error => {
        const e = error as { status?: number; name?: string };
        this.errorMessage = e.status === 403 ? 'Administrator access is required to view the full audit history.' : e.status === 401 ? 'Your session has expired. Please sign in again.' : 'Unable to load the audit history. Try again in a moment.';
        this.cdr.markForCheck();
      }
    });
  }

  getActionLabel(action: string): string {
    const labels: Record<string,string> = { Created:'Created', Updated:'Details updated', Assigned:'Assigned', Reassigned:'Reassigned', StatusChanged:'Status changed', Deleted:'Deleted' };
    return labels[action] ?? (action || 'Activity').replace(/([a-z])([A-Z])/g, '$1 $2');
  }
  getInitial(name: string | null | undefined): string { return name?.trim().charAt(0).toUpperCase() || 'U'; }
}
