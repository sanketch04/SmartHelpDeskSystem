# SmartHelpDesk — AI-Powered Support Management System

SmartHelpDesk is a web-based support management application designed to streamline ticket creation, assignment, tracking, and resolution. It provides role-based workspaces for Administrators, Support Agents, and Employees, with AI-assisted ticket classification to improve support workflows.

The frontend is built with **Angular, TypeScript, Tailwind CSS, and NgRx** and integrates with an existing ASP.NET Core Web API.

## Features

### Authentication and Access Control
- User registration and login.
- Authentication state management using NgRx.
- Role-aware navigation for Administrators, Support Agents, and Employees.
- Protected frontend routes.
- Integration with the existing JWT authentication API.

### Ticket Management
- Create and view support tickets.
- View ticket details, descriptions, priority, category, and status.
- View tickets according to the user's role.
- Assign tickets to support agents through the Administrator interface.
- Track ticket activity and audit history.
- Update ticket status through the existing API.

### AI Ticket Assistant
- AI-assisted ticket classification.
- Suggested ticket categories and priorities.
- Ticket summaries and classification explanations.
- Integration with the existing backend AI endpoint, which can connect to a locally hosted Ollama model.

### Profile Management
- Profile information display.
- Profile image display with an initial-letter fallback.
- Profile image upload through the existing API.
- Shared profile-image handling across supported UI components.

### Responsive User Interface
- Responsive layouts for desktop, tablet, and mobile.
- Sidebar navigation and mobile-friendly navigation.
- Responsive forms, ticket cards, tables, and activity history.
- Tailwind CSS utility-based styling.

## Technology Stack

| Technology | Purpose |
|---|---|
| Angular | Frontend application framework |
| TypeScript | Application logic and type safety |
| NgRx Store | Centralized application state management |
| Tailwind CSS | Responsive styling and layout |
| RxJS | Asynchronous data handling |
| ASP.NET Core Web API | Backend API integration |
| SQL Server | Backend data storage |
| Entity Framework Core | Backend data access |
| Ollama | Optional local AI model integration |

## Application Architecture

The frontend communicates with the existing backend through HTTP API services.

```text
SmartHelpDesk.Client
        |
        v
Angular Components
        |
        v
NgRx Store / Selectors
        |
        v
Angular API Services
        |
        v
ASP.NET Core Web API
        |
        +---- SQL Server
        |
        +---- Local AI Service (Ollama)
```

The backend remains responsible for authentication, authorization, business rules, database operations, and enforcing access to protected resources.

## User Roles

### Administrator
- View and manage all tickets.
- Assign and reassign tickets to support agents.
- Access administrative ticket and audit views.
- Manage support-agent workflows.

### Support Agent
- View tickets assigned to them.
- Access permitted ticket details and activity history.
- Update ticket status according to their permissions.

### Employee
- Create support tickets.
- View their own submitted tickets.
- Track ticket status and activity.

> **Security note:** Frontend route guards and role-based navigation improve the user experience but do not replace server-side authorization. Every protected operation must be validated by the backend API.

## Prerequisites

Install the following before running the project:

- [Node.js](https://nodejs.org/) — use a version compatible with the Angular version in `package.json`.
- npm, included with Node.js.
- [Angular CLI](https://angular.dev/tools/cli), if required for your local workflow.
- The SmartHelpDesk ASP.NET Core Web API.
- Access to the configured API endpoint.

If AI classification is enabled, the backend and its configured AI service must also be running.

## Getting Started

### 1. Clone the Repository

```bash
git clone <https://github.com/sanketch04/SmartHelpDeskSystem.git>
cd SmartHelpDesk.Client
```

Replace `<https://github.com/sanketch04/SmartHelpDeskSystem.git>` with your GitHub repository URL.

### 2. Install Dependencies

```bash
npm install
```

### 3. Configure the API URL

Open:

```text
src/app/core/api-config.ts
```

Configure the base URL to match your running backend:

```typescript
export const API_BASE_URL = 'https://localhost:7015/api';
```

Change this value if your API uses another host, port, or deployment URL.

### 4. Start the Angular Application

```bash
npm start
```

If the project does not define an `npm start` script, use:

```bash
npx ng serve
```

Open the local URL shown in the terminal, typically:

```text
http://localhost:4200
```

### 5. Start the Backend

Run the existing ASP.NET Core Web API separately and verify that the configured API URL is reachable.

For local development, ensure the API's CORS configuration allows requests from the Angular development origin.

## Environment and Configuration

Before deploying, review the following:

- API base URL.
- Authentication and token-handling configuration.
- CORS settings on the backend.
- Profile image URL handling.
- Production environment configuration.
- Any required AI service configuration.

Do not commit passwords, private keys, production tokens, database credentials, or other secrets to GitHub.

## Recommended Development Workflow

1. Start the backend API.
2. Start the Angular frontend.
3. Sign in with an account for the role being tested.
4. Verify ticket creation, listing, details, and status updates.
5. Verify Administrator assignment workflows.
6. Verify profile-image loading and fallback behavior.
7. Test layouts at desktop, tablet, and mobile widths.
8. Confirm that unauthorized API requests are rejected by the backend.

## Production Build

To create an optimized frontend build:

```bash
npm run build
```

If the project uses a different build script, inspect `package.json` and use the configured command.

Deploy the generated build output using an appropriate static web server or hosting service. Configure the production API URL before deployment.

## Troubleshooting

### CORS errors
Confirm that the backend permits requests from the frontend's origin and that the API is running.

### Profile images do not appear
Verify that the signed-in user has a valid `profileImageUrl`, that the URL resolves to an accessible image, and that the image endpoint is reachable from the browser.

### Tickets do not load
Check the API URL, authentication session, browser Network tab, and the HTTP status returned by the backend.

### AI classification fails
Verify that the AI endpoint is available and that the backend's configured AI service is running.

### Dependencies or build errors
Check the installed Node.js version against the project's Angular requirements, then run:

```bash
npm install
npm run build
```

## Project Goals

SmartHelpDesk aims to provide a maintainable and responsive support platform with:

- Centralized frontend state management.
- Clear role-specific user experiences.
- AI-assisted ticket classification.
- Accessible and understandable ticket activity history.
- Consistent profile-image handling.
- Secure integration with an existing backend API.

## License

Add your chosen license before distributing this project publicly. If no license has been added, the repository's reuse and redistribution permissions may be limited.

---

**SmartHelpDesk** — Simplifying support workflows through structured ticket management and AI assistance.
