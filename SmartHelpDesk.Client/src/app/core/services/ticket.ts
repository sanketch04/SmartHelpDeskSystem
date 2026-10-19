
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../api-config';
import {
  AssignTicketRequest,
  CreateTicketRequest,
  Ticket,
  TicketStatus,
  UpdateTicketRequest,
  UpdateTicketStatusRequest
} from '../../models/tickets/ticket.model';

@Injectable({
  providedIn: 'root'
})
export class TicketService {

  // #region Fields

  private readonly baseUrl = `${API_BASE_URL}/Tickets`;

  // #endregion

  // #region Ctor

  constructor(private readonly http: HttpClient) {}

  // #endregion

  // #region Get Tickets

  getAll(): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(this.baseUrl);
  }

  getMyTickets(): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(`${this.baseUrl}/my`);
  }

  getAssignedTickets(): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(`${this.baseUrl}/assigned`);
  }

  getById(id: number): Observable<Ticket> {
    return this.http.get<Ticket>(`${this.baseUrl}/${id}`);
  }

  // #endregion

  // #region Create Ticket

  create(request: CreateTicketRequest): Observable<Ticket> {
    return this.http.post<Ticket>(this.baseUrl, request);
  }

  // #endregion

  // #region Update Ticket

  update(
    id: number,
    request: UpdateTicketRequest
  ): Observable<Ticket> {
    return this.http.put<Ticket>(`${this.baseUrl}/${id}`, request);
  }

  updateStatus(
    id: number,
    status: TicketStatus
  ): Observable<Ticket> {
    const request: UpdateTicketStatusRequest = { status };

    return this.http.patch<Ticket>(
      `${this.baseUrl}/${id}/status`,
      request
    );
  }

  // #endregion

  // #region Assignment

  assign(
    id: number,
    assignedToUserId: string
  ): Observable<Ticket> {
    const request: AssignTicketRequest = { assignedToUserId };

    return this.http.put<Ticket>(
      `${this.baseUrl}/${id}/assign`,
      request
    );
  }

  // #endregion

  // #region Delete Ticket

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // #endregion

  // #region Audit History

  getAuditHistory(id: number): Observable<unknown[]> {
    return this.http.get<unknown[]>(
      `${this.baseUrl}/${id}/audit-history`
    );
  }

  // #endregion
}
