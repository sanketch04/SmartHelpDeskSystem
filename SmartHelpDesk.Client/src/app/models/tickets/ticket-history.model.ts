
export interface TicketHistory {
  id: number;
  ticketId: number;
  action: string;
  performedByUserId: string;
  performedByUserName: string;
  oldValue: string | null;
  newValue: string | null;
  description: string;
  createdAt: string;
}
