import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { Auth } from '../../../core/services/auth';
import { TicketService } from '../../../core/services/ticket';
import { Ticket, TicketPriority, TicketStatus } from '../../../models/tickets/ticket.model';

@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './ticket-list.html',
  styleUrl: './ticket-list.css'
})
export class TicketList implements OnInit {
  tickets: Ticket[] = [];
  isLoading = false;
  errorMessage = '';
  searchTerm = '';
  statusFilter = 'All';
  priorityFilter = 'All';
  readonly statuses: string[] = ['All', 'Open', 'In Progress', 'Pending', 'Resolved', 'Closed'];
  readonly priorities: string[] = ['All', 'Low', 'Medium', 'High', 'Critical'];
  viewTitle = 'Tickets';
  viewDescription = 'Track support requests and their current progress.';

  constructor(
    readonly auth: Auth,
    private readonly ticketService: TicketService,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const requestedView = this.route.snapshot.data['ticketView'] as string | undefined;
    if (requestedView === 'mine') {
      this.viewTitle = 'My Tickets';
      this.viewDescription = 'Requests you have submitted.';
      this.loadMyTickets();
    } else if (requestedView === 'assigned') {
      this.viewTitle = 'Assigned Tickets';
      this.viewDescription = 'Requests assigned to your support queue.';
      this.loadAssignedTickets();
    } else if (this.auth.isAdministrator()) {
      this.viewTitle = 'All Tickets';
      this.viewDescription = 'Review, assign, and manage all helpdesk requests.';
      this.loadAllTickets();
    } else if (this.auth.isSupportAgent()) {
      this.viewTitle = 'Assigned Tickets';
      this.viewDescription = 'Work on tickets assigned to you.';
      this.loadAssignedTickets();
    } else {
      this.viewTitle = 'My Tickets';
      this.viewDescription = 'Requests you have submitted.';
      this.loadMyTickets();
    }
  }

  get filteredTickets(): Ticket[] {
    const query = this.searchTerm.trim().toLowerCase();
    return this.tickets.filter(ticket => {
      const matchesQuery = !query ||
        `${ticket.id} ${ticket.title} ${ticket.description} ${ticket.category}`.toLowerCase().includes(query);
      const matchesStatus = this.statusFilter === 'All' || ticket.status === this.statusFilter;
      const matchesPriority = this.priorityFilter === 'All' || ticket.priority === this.priorityFilter;
      return matchesQuery && matchesStatus && matchesPriority;
    });
  }

  loadAllTickets(): void { this.load(() => this.ticketService.getAll()); }
  loadMyTickets(): void { this.load(() => this.ticketService.getMyTickets()); }
  loadAssignedTickets(): void { this.load(() => this.ticketService.getAssignedTickets()); }

  private load(request: () => import('rxjs').Observable<Ticket[]>): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();
    request().pipe(
      timeout(15000),
      finalize(() => { this.isLoading = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: response => {
        this.tickets = Array.isArray(response) ? response : [];
        if (!Array.isArray(response)) this.errorMessage = 'The API returned an unexpected response.';
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Ticket list request failed:', error);
        this.tickets = [];
        const e = error as { status?: number; name?: string; error?: { message?: string } };
        if (e.name === 'TimeoutError') this.errorMessage = 'The request timed out. Please retry.';
        else if (e.status === 0) this.errorMessage = 'Cannot reach the API. Verify the API is running, the HTTPS certificate is trusted, and CORS allows http://localhost:4200.';
        else if (e.status === 401) this.errorMessage = 'Your session has expired. Please sign in again.';
        else if (e.status === 403) this.errorMessage = 'Your account does not have permission to access this ticket view.';
        else this.errorMessage = e.error?.message || `Unable to load tickets (HTTP ${e.status ?? 'unknown'}).`;
        this.cdr.markForCheck();
      }
    });
  }

  getPriorityClass(priority: TicketPriority | string): string { return (priority || 'medium').toLowerCase(); }
  getStatusClass(status: TicketStatus | string): string { return (status || 'open').toLowerCase().replace(/\s+/g, '-'); }
  trackTicket(_index: number, ticket: Ticket): number { return ticket.id; }
}
