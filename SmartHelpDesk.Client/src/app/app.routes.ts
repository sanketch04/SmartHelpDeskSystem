import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { Dashboard } from './features/dashboard/dashboard';
import { TicketList } from './features/tickets/ticket-list/ticket-list';
import { TicketCreate } from './features/tickets/ticket-create/ticket-create';
import { TicketDetails } from './features/tickets/ticket-details/ticket-details';
import { AgentList } from './features/agents/agent-list/agent-list';
import { AuditHistory } from './features/audit-history/audit-history';
import { Profile } from './features/profile/profile';
import { AiAssistant } from './features/ai-assistant/ai-assistant';
import { AppShell } from './layout/app-shell/app-shell';

export const routes: Routes = [
  { path: 'login', component: Login, title: 'Login | SmartHelpDesk' },
  { path: 'register', component: Register, title: 'Register | SmartHelpDesk' },
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: Dashboard, title: 'Dashboard | SmartHelpDesk' },
      { path: 'tickets/create', component: TicketCreate, title: 'Create Ticket | SmartHelpDesk' },
      { path: 'tickets', component: TicketList, title: 'Tickets | SmartHelpDesk' },
      { path: 'my-tickets', component: TicketList, data: { ticketView: 'mine' }, title: 'My Tickets | SmartHelpDesk' },
      { path: 'assigned-tickets', component: TicketList, data: { ticketView: 'assigned' }, title: 'Assigned Tickets | SmartHelpDesk' },
      { path: 'tickets/:id', component: TicketDetails, title: 'Ticket Details | SmartHelpDesk' },
      { path: 'agents', component: AgentList, title: 'Support Agents | SmartHelpDesk' },
      { path: 'audit-history', component: AuditHistory, title: 'Audit History | SmartHelpDesk' },
      { path: 'ai-assistant', component: AiAssistant, title: 'AI Assistant | SmartHelpDesk' },
      { path: 'profile', component: Profile, title: 'My Profile | SmartHelpDesk' }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
