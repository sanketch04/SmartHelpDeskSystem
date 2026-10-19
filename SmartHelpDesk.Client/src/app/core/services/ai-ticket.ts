
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../api-config';
import {
  AITicketClassificationRequest,
  AITicketClassificationResponse
} from '../../models/ai/ai-ticket.model';

@Injectable({
  providedIn: 'root'
})
export class AiTicket {

  // #region Fields

  private readonly baseUrl = `${API_BASE_URL}/AI`;

  // #endregion

  // #region Ctor

  constructor(
    private readonly http: HttpClient
  ) {}

  // #endregion

  // #region Ticket Classification

  classifyTicket(
    request: AITicketClassificationRequest
  ): Observable<AITicketClassificationResponse> {
    return this.http.post<AITicketClassificationResponse>(
      `${this.baseUrl}/tickets/classify`,
      request
    );
  }

  // #endregion
}
