# SmartHelpDesk — AI-Powered Support Management System

An AI-assisted helpdesk application built to streamline support ticket creation, assignment, tracking, and resolution. SmartHelpDesk provides role-based access for Administrators, Support Agents, and Employees, along with AI-powered ticket classification.

The frontend is developed using **Angular, TypeScript, NgRx, and Tailwind CSS** and integrates with an existing ASP.NET Core Web API.

## Key Features

### Authentication and Authorization
- User registration and login.
- JWT-based authentication integration.
- Centralized frontend authentication state management using NgRx.
- Role-based navigation for Administrators, Support Agents, and Employees.
- Protected application routes.

### Ticket Management
- Create and track support tickets.
- View ticket details, descriptions, categories, priorities, and statuses.
- Access tickets according to user roles and permissions.
- Assign tickets to Support Agents through the Administrator workflow.
- Update ticket statuses.
- View ticket audit history and activity.

### AI-Powered Ticket Assistant
- AI-assisted ticket classification.
- Suggested ticket categories and priorities.
- Integration with the backend AI classification endpoint.
- Support for a locally hosted Ollama model through the backend configuration.

### Profile Management
- Display user profile information.
- Upload and display profile images.
- Resolve profile image URLs consistently across supported components.
- Display an initial-letter fallback when a profile image is unavailable.

### Responsive UI
- Responsive layouts for desktop, tablet, and mobile.
- Role-aware sidebar navigation.
- Responsive forms, ticket views, and activity timelines.
- Tailwind CSS utility-based styling.

## Technology Stack

| Technology | Purpose |
|---|---|
| Angular | Frontend framework |
| TypeScript | Type-safe application development |
| NgRx Store | Centralized state management |
| Tailwind CSS | Responsive UI styling |
| RxJS | Reactive programming and asynchronous operations |
| ASP.NET Core Web API | Backend services |
| Entity Framework Core | Backend data access |
| SQL Server | Database |
| Ollama | Local AI model integration through the backend |

## Architecture

```text
SmartHelpDesk.Client
        |
        v
Angular Components
        |
        v
NgRx Store and Selectors
        |
        v
Angular Services
        |
        v
ASP.NET Core Web API
        |
        +------ SQL Server
        |
        +------ AI Classification Service
                       |
                       +------ Ollama
```

The Angular frontend communicates with the backend through HTTP APIs. The backend handles business logic, database operations, authentication, authorization, and AI service integration.

## User Roles

### Administrator
- View and manage all support tickets.
- Assign tickets to Support Agents.
- Access permitted administrative views and audit history.
- Manage ticket workflows.

### Support Agent
- View assigned tickets.
- Access permitted ticket details and activity history.
- Update ticket statuses according to assigned permissions.

### Employee
- Create support tickets.
- View submitted tickets.
- Track ticket progress and status.

**Security:** Frontend route guards and role-based navigation are not substitutes for server-side authorization. The backend must enforce permissions for every protected API operation.

## Prerequisites

Before running the application, install:

- [Node.js](https://nodejs.org/)
- npm, included with Node.js
- The ASP.NET Core backend API
- Access to the configured API endpoint

Use a Node.js version compatible with the Angular dependencies specified in `package.json`. Angular CLI can be invoked through the project's local dependencies.

If AI classification is required, the backend's configured AI service must also be available.

## Getting Started

### 1. Clone the Repository

Open a terminal and run:

```bash
git clone https://github.com/sanketch04/SmartHelpDeskSystem.git
cd SmartHelpDeskSystem
```

Navigate to the frontend project directory containing `package.json` and `angular.json`.

For example, if the frontend is stored in a subdirectory:

```bash
cd SmartHelpDesk.Client
```

Adjust the directory name if your repository uses a different structure.

### 2. Install Dependencies

From the frontend directory, run:

```bash
npm install
```

### 3. Configure the Backend API

Open:

```text
src/app/core/api-config.ts
```

Configure the API base URL to match your running ASP.NET Core backend:

```typescript
export const API_BASE_URL = 'https://localhost:7015/api';
```

The URL above is an example of the local development configuration. Update it if your backend uses a different host or port.

### 4. Start the Backend

Run the SmartHelpDesk ASP.NET Core Web API separately.

Verify that the API is running and that its CORS policy allows requests from the Angular development origin.

For local HTTPS development, ensure the backend's HTTPS development certificate is trusted by your browser.

### 5. Start the Frontend

From the frontend project directory, run:

```bash
npm start
```

If the project does not define a `start` script in `package.json`, use:

```bash
npx ng serve
```

Open the local development URL displayed in the terminal. The default Angular development URL is usually:

```text
http://localhost:4200
```

## Configuration

Before running or deploying the application, verify:

- The backend API base URL.
- Authentication and session-handling configuration.
- Backend CORS settings.
- Profile image URL resolution.
- Production environment settings.
- AI service availability and configuration.

**Never commit credentials, access tokens, private keys, database passwords, or other secrets to the repository.**

## Production Build

To create a production build, run:

```bash
npm run build
```

Use the build script defined in the frontend's `package.json`. The generated output directory is determined by the Angular workspace configuration.

Before deployment:

1. Configure the correct production API URL.
2. Verify authentication and authorization workflows.
3. Test profile image loading.
4. Confirm that API requests use the intended production endpoint.
5. Test the application on desktop, tablet, and mobile screen sizes.
6. Configure your web server to serve the Angular application and handle client-side routes correctly.

## Troubleshooting

### API requests fail
- Confirm that the backend is running.
- Verify the API base URL.
- Check the browser's Network and Console panels.
- Confirm that the backend permits the frontend origin through CORS.

### Login or authentication fails
- Verify the API endpoint and credentials.
- Check the HTTP response returned by the backend.
- Confirm that the authentication session is being stored and read correctly.

### Profile images do not display
- Verify that the API returns a valid profile image URL.
- Confirm that the image endpoint is accessible.
- Check whether the URL is relative or absolute and that it resolves to the correct backend origin.

### AI classification fails
- Confirm that the backend AI endpoint is reachable.
- Verify the backend's AI service configuration.
- Ensure that the configured Ollama model is available when using local inference.

### Dependency or build errors
Check the installed Node.js version and run:

```bash
npm install
npm run build
```

Review the terminal output for dependency, TypeScript, or Angular configuration errors.

## Development Guidelines

- Keep API communication inside Angular services.
- Use NgRx for the application state managed by the Store.
- Keep components focused on presentation and user interaction.
- Use reusable components and consistent Tailwind CSS styling.
- Preserve the existing backend API contracts.
- Validate authorization on the backend rather than relying only on frontend role checks.
- Keep configuration and secrets out of source control.

## Project Objectives

SmartHelpDesk aims to deliver a maintainable support management application with:

- Centralized frontend state management.
- Role-based support workflows.
- AI-assisted ticket classification.
- Ticket activity tracking and audit history.
- Consistent profile image handling.
- Responsive and accessible user interfaces.
- Secure integration with an existing ASP.NET Core backend.

## License

A license has not been specified in this README. Add an appropriate `LICENSE` file to clarify the terms under which others may use, modify, and distribute the project.

---

**SmartHelpDesk** — Simplifying support workflows through structured ticket management and AI assistance.
