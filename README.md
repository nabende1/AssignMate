# AssignMate

AssignMate is a focused student assignment tracker built with .NET 10 Blazor Web App and interactive server rendering. It provides a calm workspace for capturing assignments, reviewing deadlines, and keeping semester progress visible.

## Features

- Dashboard with open, due-soon, completed, and completion-rate metrics
- Assignment creation with title, course, due date, priority, notes, and validation
- Task search, status, upcoming, and overdue filtering
- Edit assignments and track creation/modification timestamps
- One-click completion and deletion
- Editable student profile with instant UI updates
- Email/password registration, sign-in, cookie sessions, and sign-out
- Per-user data isolation backed by SQLite and Entity Framework Core
- Responsive layout for desktop and mobile screens
- Accessible labels, focus states, semantic page structure, and useful empty states

## Requirements

- .NET 10 SDK
- A modern browser with JavaScript enabled
- SQLite for zero-setup local development, or SQL Server for production

## Run locally

```powershell
dotnet restore
dotnet run
```

Open the HTTPS URL printed by the application, normally `https://localhost:7243`.

On first startup, EF Core applies the checked-in migration and creates `assignmate.db` with its Identity/task tables. Register a user at `/register`, then sign in at `/login`.

To verify a production build:

```powershell
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
```

## Architecture

- `Components/Pages` contains the interactive user workflows.
- `Models/TaskItem.cs` contains the task and profile domain types.
- `Data/ApplicationDbContext.cs` owns the relational schema, Identity tables, and task relationship.
- `Data/Migrations` contains the reviewed EF Core schema migration.
- `Data/ApplicationUser.cs` stores account profile fields.
- `Services/TaskStore.cs` owns authenticated, user-scoped task mutations.
- `wwwroot/app.css` contains the responsive visual system.

SQLite is the default local database. For SQL Server, set `DatabaseProvider` to `SqlServer` and set `ConnectionStrings:SqlServerConnection` to a managed SQL Server connection string, or use `DefaultConnection` as a fallback. Apply migrations during deployment with `dotnet ef database update` or the application's startup migration step.
## Docker

This repository includes a `Dockerfile` for container deployment on platforms like Render.

Build locally:

```powershell
docker build -t assignmate .
```

Run locally:

```powershell
docker run -p 8080:80 assignmate
```

## Launching on Render

Use Docker as the runtime environment and connect your GitHub repo. The default build and start commands are handled by Render when Docker is selected.

Add these environment variables in Render:

- `ASPNETCORE_ENVIRONMENT = Production`
- `ASPNETCORE_URLS = http://*:$PORT`
- `DatabaseProvider = Sqlite`
- `ConnectionStrings__DefaultConnection = Data Source=assignmate.db`
## Testing

- `dotnet test AssignMate.Tests/AssignMate.Tests.csproj`

## Production checklist

1. Configure HTTPS certificates and a managed `ASPNETCORE_ENVIRONMENT`.
2. Configure a managed database connection and replace `EnsureCreated` with reviewed EF Core migrations.
3. Configure cookie key persistence/shared storage when running multiple instances.
4. Set explicit `AllowedHosts` and configure structured logging/health checks for the hosting environment.
5. Run `dotnet build --configuration Release` and review the publish output before deployment.
