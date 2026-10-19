
export interface AITicketClassificationRequest {
  title: string;
  description: string;
}

export interface AITicketClassificationResponse {
  suggestedCategory: string;
  suggestedPriority: 'Low' | 'Medium' | 'High' | 'Critical';
  summary: string;
  reason: string;
}
