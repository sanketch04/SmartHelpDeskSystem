
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export type TicketStatus =
  | 'Open'
  | 'In Progress'
  | 'Pending'
  | 'Resolved'
  | 'Closed';

export interface Ticket {
  id: number;
  title: string;
  description: string;
  category: string;
  priority: TicketPriority;
  status: TicketStatus;
  createdAt: string;
  createdByUserId: string;
  assignedToUserId: string | null;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  category: string;
  priority: TicketPriority;
}

export interface UpdateTicketRequest {
  title: string;
  description: string;
  category: string;
  priority: TicketPriority;
}

export interface AssignTicketRequest {
  assignedToUserId: string;
}

export interface UpdateTicketStatusRequest {
  status: TicketStatus;
}
