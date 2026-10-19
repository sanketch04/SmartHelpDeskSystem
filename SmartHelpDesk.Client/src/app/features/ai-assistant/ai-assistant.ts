import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { AiTicket } from '../../core/services/ai-ticket';
import { AITicketClassificationResponse } from '../../models/ai/ai-ticket.model';

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './ai-assistant.html',
  styleUrl: './ai-assistant.css'
})
export class AiAssistant {
  private readonly fb = inject(FormBuilder);
  readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(5000)]]
  });
  isLoading = false;
  errorMessage = '';
  result: AITicketClassificationResponse | null = null;
  constructor(private readonly ai: AiTicket) {}
  classify(): void {
    this.form.markAllAsTouched();
    this.errorMessage = '';
    this.result = null;
    if (this.form.invalid || this.isLoading) return;
    const value = this.form.getRawValue();
    this.isLoading = true;
    this.ai.classifyTicket({ title: value.title!.trim(), description: value.description!.trim() })
      .pipe(timeout(120000), finalize(() => this.isLoading = false))
      .subscribe({
        next: result => this.result = result,
        error: error => {
          const e = error as { status?: number; name?: string; error?: { message?: string } };
          this.errorMessage = e.name === 'TimeoutError'
            ? 'The AI request took too long. Please try again.'
            : e.status === 0 || e.status === 503
              ? 'The AI service is unavailable. Check that the API and local Ollama service are running.'
              : e.error?.message || 'Unable to classify this request.';
        }
      });
  }
}
