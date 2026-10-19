import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-config';

export interface SupportAgentSummary {
  id: string;
  supportAgentCode: string | null;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  isActive: boolean;
  createdAt: string;
}
export interface CreateSupportAgentRequest {
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  password: string;
}
@Injectable({ providedIn: 'root' })
export class SupportAgent {
  private readonly baseUrl = `${API_BASE_URL}/SupportAgents`;
  constructor(private readonly http: HttpClient) {}
  getAll(): Observable<SupportAgentSummary[]> { return this.http.get<SupportAgentSummary[]>(this.baseUrl); }
  create(request: CreateSupportAgentRequest): Observable<SupportAgentSummary> { return this.http.post<SupportAgentSummary>(this.baseUrl, request); }
  deactivate(email: string): Observable<unknown> { return this.http.put(`${this.baseUrl}/deactivate`, null, { params: { email } }); }
  activate(email: string): Observable<unknown> { return this.http.put(`${this.baseUrl}/activate`, null, { params: { email } }); }
}
