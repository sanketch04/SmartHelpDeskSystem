import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { finalize, TimeoutError, timeout } from 'rxjs';

import { AiTicket } from '../../../core/services/ai-ticket';
import { TicketService } from '../../../core/services/ticket';

import { TicketPriority } from '../../../models/tickets/ticket.model';
import { AITicketClassificationResponse } from '../../../models/ai/ai-ticket.model';

@Component({
  selector: 'app-ticket-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-create.html',
  styleUrl: './ticket-create.css',
})
export class TicketCreate {
  private readonly formBuilder = inject(FormBuilder);

  // #region Fields

  readonly priorities: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];

  isClassifying = false;
  isSubmitting = false;

  aiError = '';
  submitError = '';
  successMessage = '';

  aiSuggestion: AITicketClassificationResponse | null = null;
  aiSuggestionApplied = false;

  readonly ticketForm = this.formBuilder.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(5000)]],
    category: ['General', [Validators.required, Validators.maxLength(100)]],
    priority: ['Medium' as TicketPriority, Validators.required],
  });

  // #endregion

  // #region Ctor

  constructor(
    private readonly aiTicket: AiTicket,
    private readonly ticketService: TicketService,
    private readonly router: Router,
  ) {}

  // #endregion

  // #region Form Helpers

  get title() {
    return this.ticketForm.controls.title;
  }

  get description() {
    return this.ticketForm.controls.description;
  }

  get category() {
    return this.ticketForm.controls.category;
  }

  get priority() {
    return this.ticketForm.controls.priority;
  }

  // #endregion

  // #region Input Changes

  onTicketContentChanged(): void {
    this.aiSuggestion = null;
    this.aiSuggestionApplied = false;
    this.aiError = '';
    this.submitError = '';
  }

  // #endregion

  // #region AI Classification

  getAiSuggestions(): void {
    this.aiError = '';
    this.submitError = '';
    this.successMessage = '';
    this.aiSuggestion = null;
    this.aiSuggestionApplied = false;

    this.title.markAsTouched();
    this.description.markAsTouched();

    if (this.title.invalid || this.description.invalid) {
      return;
    }

    if (this.isClassifying || this.isSubmitting) {
      return;
    }

    this.isClassifying = true;

    this.aiTicket
      .classifyTicket({
        title: this.title.value!.trim(),
        description: this.description.value!.trim(),
      })
      .pipe(
        // Local AI inference can take longer than a normal API call.
        timeout(60000),
        finalize(() => {
          this.isClassifying = false;
        }),
      )
      .subscribe({
        next: (response) => {
          if (!response) {
            this.aiError = 'The AI service returned an empty response.';
            return;
          }

          this.aiSuggestion = response;
        },
        error: (error: unknown) => {
          console.error('AI ticket classification failed:', error);

          const httpError = error as HttpErrorResponse & {
            name?: string;
          };

          if (error instanceof TimeoutError) {
            this.aiError =
              'AI analysis took too long. Please check that Ollama is running and try again.';
          } else if (httpError.status === 0) {
            this.aiError =
              'Cannot connect to the API. Check that the backend is running and CORS is configured correctly.';
          } else if (httpError.status === 401) {
            this.aiError = 'Your session may have expired. Please log in again.';
          } else if (httpError.status === 403) {
            this.aiError = 'Your account does not have permission to use AI classification.';
          } else if (httpError.status === 503) {
            this.aiError =
              'The AI service is temporarily unavailable. Check that Ollama and the configured model are available.';
          } else {
            this.aiError =
              httpError.error?.message ||
              'Unable to generate AI suggestions. You can still create the ticket manually.';
          }
        },
      });
  }

  applyAiSuggestions(): void {
    if (!this.aiSuggestion) {
      return;
    }

    const suggestedCategory = this.aiSuggestion.suggestedCategory?.trim();

    const suggestedPriority = this.aiSuggestion.suggestedPriority;

    if (suggestedCategory) {
      this.category.setValue(suggestedCategory);
      this.category.markAsDirty();
      this.category.markAsTouched();
    }

    if (this.priorities.includes(suggestedPriority)) {
      this.priority.setValue(suggestedPriority);
      this.priority.markAsDirty();
      this.priority.markAsTouched();
    }

    this.aiSuggestionApplied = true;
    this.submitError = '';
  }

  keepMyValues(): void {
    this.aiSuggestionApplied = false;
    this.aiSuggestion = null;
    this.aiError = '';
  }

  // #endregion

  // #region Create Ticket

  createTicket(): void {
    this.submitError = '';
    this.successMessage = '';

    this.ticketForm.markAllAsTouched();

    if (this.ticketForm.invalid) {
      this.submitError = 'Please correct the validation errors before creating the ticket.';
      return;
    }

    if (this.isSubmitting || this.isClassifying) {
      return;
    }

    const formValue = this.ticketForm.getRawValue();

    this.isSubmitting = true;

    this.ticketService
      .create({
        title: formValue.title!.trim(),
        description: formValue.description!.trim(),
        category: formValue.category!.trim(),
        priority: formValue.priority as TicketPriority,
      })
      .pipe(
        timeout(30000),
        finalize(() => {
          this.isSubmitting = false;
        }),
      )
      .subscribe({
        next: (ticket) => {
          this.successMessage = `Ticket #${ticket.id} created successfully.`;

          void this.router.navigate(['/tickets', ticket.id]);
        },
        error: (error: unknown) => {
          console.error('Ticket creation failed:', error);

          const httpError = error as HttpErrorResponse & {
            name?: string;
          };

          if (error instanceof TimeoutError) {
            this.submitError =
              'The request took too long. Check your tickets before retrying to avoid creating a duplicate.';
          } else if (httpError.status === 0) {
            this.submitError = 'Cannot connect to the API. Check that the backend is running.';
          } else if (httpError.status === 400) {
            this.submitError =
              httpError.error?.message ||
              'The API rejected the ticket data. Check the title, description, category, and priority.';
          } else if (httpError.status === 401) {
            this.submitError = 'Your session has expired. Please log in again.';
          } else if (httpError.status === 403) {
            this.submitError = 'You do not have permission to create tickets.';
          } else {
            this.submitError =
              httpError.error?.message ||
              `Ticket creation failed. HTTP status: ${httpError.status || 'unknown'}.`;
          }
        },
      });
  }

  // #endregion
}
