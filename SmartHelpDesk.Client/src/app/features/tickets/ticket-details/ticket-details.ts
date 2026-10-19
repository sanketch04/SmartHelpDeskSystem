import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { Auth } from '../../../core/services/auth';
import { SupportAgent, SupportAgentSummary } from '../../../core/services/support-agent';
import { TicketService } from '../../../core/services/ticket';
import { Ticket, TicketStatus } from '../../../models/tickets/ticket.model';
import { TicketHistory } from '../../../models/tickets/ticket-history.model';

interface DisplayChange { label: string; value: string; }

@Component({
  selector: 'app-ticket-details',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './ticket-details.html',
  styleUrl: './ticket-details.css'
})
export class TicketDetails implements OnInit {
  ticket: Ticket | null = null;
  auditHistory: TicketHistory[] = [];
  agents: SupportAgentSummary[] = [];
  selectedAgentId = '';
  selectedStatus: TicketStatus = 'Open';
  readonly statuses: TicketStatus[] = ['Open', 'In Progress', 'Pending', 'Resolved', 'Closed'];
  isLoading = false;
  isHistoryLoading = false;
  isAgentsLoading = false;
  isSavingAssignment = false;
  isSavingStatus = false;
  errorMessage = '';
  historyError = '';
  actionMessage = '';
  actionError = '';

  private readonly displayFieldNames: Record<string, string> = {
    Title: 'Ticket title', Description: 'Description', Category: 'Category',
    Priority: 'Priority', Status: 'Status', AssignedToUserId: 'Assignment'
  };

  constructor(
    readonly auth: Auth,
    private readonly route: ActivatedRoute,
    private readonly ticketService: TicketService,
    private readonly supportAgentService: SupportAgent,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.retryLoadTicket(); }

  loadTicket(id: number): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.historyError = '';
    this.actionError = '';
    this.actionMessage = '';
    this.ticket = null;
    this.auditHistory = [];
    this.cdr.markForCheck();

    this.ticketService.getById(id).pipe(
      timeout(15000),
      finalize(() => { this.isLoading = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: ticket => {
        this.ticket = ticket;
        this.selectedStatus = ticket.status;
        this.selectedAgentId = ticket.assignedToUserId ?? '';
        this.cdr.markForCheck();
        this.loadAuditHistory(id);
        if (this.auth.isAdministrator()) this.loadAgents();
      },
      error: (error: unknown) => {
        console.error('Failed to load ticket details:', error);
        const e = error as { status?: number; name?: string };
        if (e.name === 'TimeoutError') this.errorMessage = 'The request took too long. Please try again.';
        else if (e.status === 0) this.errorMessage = 'Cannot connect to the API. Check the API URL, HTTPS certificate, and CORS settings.';
        else if (e.status === 401) this.errorMessage = 'Your session has expired. Please sign in again.';
        else if (e.status === 403) this.errorMessage = 'Your account is not allowed to view this ticket.';
        else if (e.status === 404) this.errorMessage = 'This ticket could not be found.';
        else this.errorMessage = `Unable to load ticket details (HTTP ${e.status ?? 'unknown'}).`;
        this.cdr.markForCheck();
      }
    });
  }

  retryLoadTicket(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.ticket = null;
      this.isLoading = false;
      this.errorMessage = 'The ticket ID in the URL is invalid.';
      this.cdr.markForCheck();
      return;
    }
    this.loadTicket(id);
  }

  loadAgents(): void {
    this.isAgentsLoading = true;
    this.supportAgentService.getAll().pipe(
      timeout(15000),
      finalize(() => { this.isAgentsLoading = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: agents => { this.agents = Array.isArray(agents) ? agents : []; this.cdr.markForCheck(); },
      error: error => {
        console.error('Unable to load support agents:', error);
        this.actionError = 'Support agents could not be loaded. Check Administrator access and retry.';
        this.cdr.markForCheck();
      }
    });
  }

  saveAssignment(): void {
    if (!this.ticket || !this.auth.isAdministrator() || !this.selectedAgentId || this.isSavingAssignment) return;
    this.actionError = '';
    this.actionMessage = '';
    this.isSavingAssignment = true;
    this.ticketService.assign(this.ticket.id, this.selectedAgentId).pipe(
      timeout(15000),
      finalize(() => { this.isSavingAssignment = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: updated => {
        this.ticket = updated;
        this.selectedAgentId = updated.assignedToUserId ?? '';
        this.actionMessage = 'Ticket assignment saved. The activity log has been refreshed.';
        this.loadAuditHistory(updated.id);
        this.cdr.markForCheck();
      },
      error: error => {
        const e = error as { status?: number; error?: { message?: string } };
        this.actionError = e.error?.message || (e.status === 403
          ? 'Only an Administrator can assign tickets.'
          : `Assignment could not be saved (HTTP ${e.status ?? 'unknown'}).`);
        this.cdr.markForCheck();
      }
    });
  }

  saveStatus(): void {
    if (!this.ticket || this.isSavingStatus || this.selectedStatus === this.ticket.status) return;
    this.actionError = '';
    this.actionMessage = '';
    this.isSavingStatus = true;
    this.ticketService.updateStatus(this.ticket.id, this.selectedStatus).pipe(
      timeout(15000),
      finalize(() => { this.isSavingStatus = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: updated => {
        this.ticket = updated;
        this.selectedStatus = updated.status;
        this.actionMessage = 'Ticket status updated successfully.';
        this.loadAuditHistory(updated.id);
        this.cdr.markForCheck();
      },
      error: error => {
        const e = error as { status?: number; error?: { message?: string } };
        this.actionError = e.error?.message || (e.status === 403
          ? 'Your account does not have permission to update this ticket status.'
          : `Status could not be updated (HTTP ${e.status ?? 'unknown'}).`);
        this.cdr.markForCheck();
      }
    });
  }

  loadAuditHistory(id: number): void {
    this.isHistoryLoading = true;
    this.historyError = '';
    this.cdr.markForCheck();
    this.ticketService.getAuditHistory(id).pipe(
      timeout(15000),
      finalize(() => { this.isHistoryLoading = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: history => {
        if (!Array.isArray(history)) {
          this.auditHistory = [];
          this.historyError = 'The API returned an unexpected activity-history response.';
        } else this.auditHistory = history as TicketHistory[];
        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Failed to load audit history:', error);
        const e = error as { status?: number; name?: string };
        this.auditHistory = [];
        this.historyError = e.status === 403
          ? 'You do not have permission to view this activity history.'
          : 'Activity history is temporarily unavailable. Ticket details remain available.';
        this.cdr.markForCheck();
      }
    });
  }

  getDisplayChanges(rawValue: string | null | undefined): DisplayChange[] {
    if (!rawValue?.trim()) return [];
    let parsed: unknown;
    try {
      parsed = JSON.parse(rawValue);
      if (typeof parsed === 'string') parsed = JSON.parse(parsed);
    } catch { return []; }
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return [];
    const data = parsed as Record<string, unknown>;
    return Object.entries(this.displayFieldNames)
      .filter(([key]) => Object.prototype.hasOwnProperty.call(data, key))
      .map(([key, label]) => ({ label, value: this.formatAuditValue(data[key], key) }))
      .filter(item => item.value.length > 0);
  }

  private formatAuditValue(value: unknown, fieldName: string): string {
    if (value === null || value === undefined || value === '') {
      return fieldName === 'AssignedToUserId' ? 'Unassigned' : 'Not specified';
    }
    if (fieldName === 'AssignedToUserId') return 'Assignment updated';
    return typeof value === 'object' ? '' : String(value);
  }

  getActionLabel(action: string | null | undefined): string {
    if (!action?.trim()) return 'Activity';
    const labels: Record<string, string> = {
      Created: 'Created', Updated: 'Details updated', Deleted: 'Deleted',
      Assigned: 'Assigned', Reassigned: 'Reassigned', StatusChanged: 'Status changed',
      PriorityChanged: 'Priority changed'
    };
    return labels[action] ?? action.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  getInitial(name: string | null | undefined): string {
    return name?.trim().charAt(0).toUpperCase() || 'U';
  }
  getPriorityClass(priority: string): string { return (priority || 'medium').toLowerCase(); }
  getStatusClass(status: string): string { return (status || 'open').toLowerCase().replace(/\s+/g, '-'); }
}
