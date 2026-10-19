import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { SupportAgent, SupportAgentSummary } from '../../../core/services/support-agent';

@Component({
  selector: 'app-agent-list',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule],
  templateUrl: './agent-list.html',
  styleUrl: './agent-list.css'
})
export class AgentList implements OnInit {
  agents: SupportAgentSummary[] = [];
  isLoading = false;
  isSaving = false;
  showCreateForm = false;
  errorMessage = '';
  successMessage = '';
  searchTerm = '';
  form = { fullName: '', email: '', phoneNumber: '', password: '' };

  constructor(private readonly service: SupportAgent, private readonly cdr: ChangeDetectorRef) {}
  ngOnInit(): void { this.loadAgents(); }

  get filteredAgents(): SupportAgentSummary[] {
    const q = this.searchTerm.trim().toLowerCase();
    return this.agents.filter(a => !q || `${a.fullName} ${a.email} ${a.supportAgentCode ?? ''}`.toLowerCase().includes(q));
  }

  loadAgents(): void {
    this.isLoading = true; this.errorMessage = '';
    this.service.getAll().pipe(timeout(15000), finalize(() => { this.isLoading = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: agents => { this.agents = Array.isArray(agents) ? agents : []; this.cdr.markForCheck(); },
        error: error => {
          const e = error as { status?: number; error?: { message?: string } };
          this.errorMessage = e.status === 403 ? 'Administrator access is required to manage support agents.' : e.error?.message || 'Unable to load support agents.';
          this.cdr.markForCheck();
        }
      });
  }

  createAgent(): void {
    this.errorMessage = ''; this.successMessage = '';
    if (!this.form.fullName.trim() || !this.form.email.trim() || this.form.password.length < 8) {
      this.errorMessage = 'Enter a name, valid email, and password with at least 8 characters.';
      return;
    }
    this.isSaving = true;
    this.service.create({
      fullName: this.form.fullName.trim(),
      email: this.form.email.trim(),
      phoneNumber: this.form.phoneNumber.trim() || null,
      password: this.form.password
    }).pipe(timeout(15000), finalize(() => { this.isSaving = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: agent => {
          this.successMessage = `Support agent ${agent.fullName} was created.`;
          this.form = { fullName: '', email: '', phoneNumber: '', password: '' };
          this.showCreateForm = false;
          this.loadAgents();
        },
        error: error => {
          const e = error as { status?: number; error?: { message?: string; errors?: string[] } };
          this.errorMessage = e.error?.message || e.error?.errors?.join(' ') || (e.status === 409 ? 'An account already uses this email.' : 'Unable to create the support agent.');
          this.cdr.markForCheck();
        }
      });
  }

  toggleAgent(agent: SupportAgentSummary): void {
    this.errorMessage = ''; this.successMessage = '';
    const request = agent.isActive ? this.service.deactivate(agent.email) : this.service.activate(agent.email);
    request.pipe(timeout(15000), finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => { this.successMessage = `${agent.fullName} ${agent.isActive ? 'deactivated' : 'activated'} successfully.`; this.loadAgents(); },
      error: error => {
        const e = error as { error?: { message?: string } };
        this.errorMessage = e.error?.message || 'Unable to update this agent.';
      }
    });
  }
}
