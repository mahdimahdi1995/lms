# LMS — Enterprise Learning Management System

> A full-stack, production-patterned LMS built end-to-end: clean architecture .NET backend,
> modern Angular frontend, local Docker development in week 1, Azure cloud deployment in week 2.

<!-- CI badge — added after Auth slice -->

## What This Is

An enterprise-grade LMS covering real-world B2B training platform concerns:

- **Clean Architecture** — Domain / Application / Infrastructure / Api layers
- **Modern Angular** — standalone components, signals, Angular Material M3, lazy-loaded routes
- **Enterprise LMS domain** — Learning Paths, Approval Workflows, role-based access (Learner / Trainer / Manager / Admin)
- **Green CI from day one** — GitHub Actions on every push and PR
- **Azure deployment** — Azure App Service + Static Web Apps + SQL Database, all free tier, OIDC-authenticated deployment

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full picture. Brief summary:

- **Backend**: ASP.NET 9 Web API — clean/layered solution (Domain → Application → Infrastructure → Api)
- **Frontend**: Angular (latest) with Angular Material, standalone components, feature-based folder structure
- **Database**: SQL Server 2022 in Docker (local) → Azure SQL Database (production, free offer)
- **Auth**: ASP.NET Core Identity + JWT; Microsoft Entra ID OAuth seam designed in from day one

## Tech Stack

| Layer | Technology | Why |
|-------|-----------|-----|
| Backend API | ASP.NET 9 Web API | Modern, minimal, high-performance .NET |
| ORM | EF Core 9 | Industry standard; rich migration tooling |
| Auth | ASP.NET Core Identity + JWT | Proven, extensible; Entra ID seam built in |
| Mapping | Mapperly | Source-generated, zero-reflection; compile-time errors |
| Validation | FluentValidation | Expressive, testable, standard in .NET enterprise |
| Frontend | Angular (latest) + Material | Standalone components, signals, M3 theming |
| Database (local) | SQL Server 2022 (Docker) | Mirrors production; zero installation |
| Database (cloud) | Azure SQL Database | Free offer (serverless, 32 GB); same SQL Server dialect |
| API hosting | Azure App Service F1 | Free tier; GitHub Actions deploy |
| Frontend hosting | Azure Static Web Apps | Free tier; native GitHub Actions integration |
| Secrets (cloud) | Azure Key Vault | Free tier; replaces appsettings secrets in production |
| CI/CD | GitHub Actions + OIDC | No stored Azure credentials; enterprise-standard pattern |
| Testing | xUnit + FluentAssertions + WebApplicationFactory | Fast, CI-friendly integration tests |

## How to Run Locally

**Prerequisites:** Docker Desktop · .NET 9 SDK · Node.js 20+ · Angular CLI

```bash
# 1. Start SQL Server
docker compose up -d

# 2. Apply migrations and seed data
dotnet ef database update --project src/LMS.Infrastructure --startup-project src/LMS.Api

# 3. Start the API  (Swagger UI: https://localhost:5001/swagger)
dotnet run --project src/LMS.Api

# 4. Start the frontend  (http://localhost:4200)
cd frontend/lms-ui && npm install && ng serve
```

## How to Run Tests

```bash
# Backend (unit + integration)
dotnet test

# Frontend
cd frontend/lms-ui && ng test --watch=false
```

## CI/CD

See [CI.md](CI.md) for pipeline details.

- **CI**: runs on every push/PR — build backend, run all tests, build Angular
- **CD (week 2)**: GitHub Actions OIDC → Azure App Service (API) + Azure Static Web Apps (frontend)

## Feature Status

| # | Feature | Status | Week |
|---|---------|--------|------|
| 1 | Auth — register, login, JWT, 4 roles | 📋 Planned | 1 |
| 2 | GitHub Actions CI | 📋 Planned | 1 |
| 3 | User management (Admin CRUD, soft-delete, team scope) | 📋 Planned | 1 |
| 4 | Course catalog (Trainer CRUD, draft/published, skill tags) | 📋 Planned | 1 |
| 5 | Lessons (ordered, markdown, optional video) | 📋 Planned | 1 |
| 6 | Enrollment with approval workflow | 📋 Planned | 1 |
| 7 | Progress tracking (lesson complete, % per course) | 📋 Planned | 1 |
| 8 | Quizzes (per-lesson, multiple-choice, auto-graded) | 📋 Planned | 1 |
| A | Azure SQL Database + Key Vault | 📋 Planned | 2 |
| B | Azure App Service deployment (API) | 📋 Planned | 2 |
| C | Azure Static Web Apps deployment (Angular) | 📋 Planned | 2 |
| D | GitHub Actions CD — OIDC to Azure | 📋 Planned | 2 |
| E | Microsoft Entra ID OAuth (second auth provider) | 🔵 Stretch | 2 |
| F | Learning Paths (ordered courses with prerequisites) | 🔵 Stretch | — |
| G | Skill profile | 🔵 Stretch | — |
| H | Dashboards (Learner / Manager / Admin views) | 🔵 Stretch | — |

✅ Done · 🚧 In progress · 📋 Planned · 🔵 Stretch

## Seed Data

Development startup auto-seeds:

- One user per role (Learner, Trainer, Manager, Admin) with known dev passwords
- 3+ courses across draft/published states with skill tags
- 2+ Learning Paths with course prerequisites
- Enrollments in pending, approved, and rejected states
- Progress records and a completed quiz attempt

## What I'd Add Next

Stretch features are designed in from day one — not bolted on. Entra ID OAuth slots into
`IAuthService` without touching business logic. Learning Paths build on the existing
enrollment state machine. Dashboards compose existing progress and enrollment data.
