# SmartHelpDesk Angular Frontend

This ZIP contains **frontend files only**. The .NET backend project and backend source files have not been modified or included.

## Features included

- Role-aware dashboard and navigation for Administrator, SupportAgent, and Employee.
- Ticket lists choose the existing API endpoint based on the signed-in role:
  - Administrator: `GET /api/Tickets`
  - SupportAgent: `GET /api/Tickets/assigned`
  - Employee: `GET /api/Tickets/my`
- Ticket details and customer-friendly activity history (raw audit JSON is not rendered to clients).
- Administrator ticket assignment using the existing `GET /api/SupportAgents` and `PUT /api/Tickets/{id}/assign` endpoints.
- Ticket status update UI using the existing `PATCH /api/Tickets/{id}/status` endpoint.
- Administrator support-agent directory and create/deactivate actions using existing API endpoints.
- AI classification assistant using the existing `POST /api/AI/tickets/classify` endpoint.
- Profile details and profile-image upload using the existing `PUT /api/ProfileImages/me` endpoint.
- Search and filters for ticket and agent lists.

## Run locally

1. Open this frontend folder in Visual Studio Code or a terminal.
2. Install dependencies using `npm install`.
3. Confirm `src/app/core/api-config.ts` points to your running backend. The current setting is `https://localhost:7015/api`.
4. Start Angular with `npm start`.
5. Browse to `http://localhost:4200`.

## Important authorization note

The frontend reads role claims from the access token to display relevant navigation and controls. This is **only a user-interface convenience**, not a security boundary. Your existing .NET API must continue to enforce authorization and resource-level access for every request. No backend code has been changed in this deliverable.

## Validation

TypeScript and Angular template compilation were checked with the Angular compiler. A full `ng build` could not be executed in this Linux build environment because the uploaded `node_modules` contains the Windows-only esbuild binary; run `npm install` on your Windows development machine before `npm start` / `npm run build` to install the correct platform dependencies.
