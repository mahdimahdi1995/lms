# Build Plan — Enterprise LMS

## Documentation Sync Map

Every slice is done only when tests pass **and** affected docs are updated. This table is
the reference — check it at the end of every feature before moving on.

| If this changes in code... | Update these documents |
|---------------------------|------------------------|
| New entity or relationship | `SCHEMA.md` — add to ER diagram and Key Design Decisions |
| New layer, seam, or project structure | `ARCHITECTURE.md` — update diagram and responsibilities |
| New API endpoint | `API.md` (started per slice) — add method, route, auth, example |
| Feature completed | `README.md` — change 📋 Planned → ✅ Done in feature status table |
| New business rule or role permission | `DOMAIN.md` — update entity's business rules section |
| New tech decision, gotcha, or pattern | `NOTES.md` — add under the relevant heading |
| Azure config or deployment change | `ARCHITECTURE.md` Azure section + `PLAN.md` Azure steps |
| Test approach changed | `TESTING.md` — update the relevant priority section |

## Solution Structure

```
lms/
├── .github/
│   └── workflows/
│       ├── ci.yml.template          # Outline (plan phase); full version after Auth slice
│       └── ci.yml                   # Added after Auth slice
├── src/
│   ├── LMS.sln
│   ├── LMS.Domain/                  # Entities, enums, domain exceptions — NO framework deps
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Course.cs
│   │   │   ├── Module.cs
│   │   │   ├── Lesson.cs
│   │   │   ├── LearningPath.cs
│   │   │   ├── LearningPathCourse.cs
│   │   │   ├── Skill.cs
│   │   │   ├── Enrollment.cs
│   │   │   ├── Progress.cs
│   │   │   ├── Quiz.cs
│   │   │   ├── QuizQuestion.cs
│   │   │   ├── QuizOption.cs
│   │   │   └── QuizAttempt.cs
│   │   ├── Enums/
│   │   │   ├── UserRole.cs          # Learner | Trainer | Manager | Admin
│   │   │   ├── CourseStatus.cs      # Draft | Published
│   │   │   └── EnrollmentStatus.cs  # Pending | Approved | Rejected
│   │   └── LMS.Domain.csproj
│   ├── LMS.Application/             # Handlers, DTOs, validators, interfaces
│   │   ├── Auth/
│   │   │   ├── RegisterUserHandler.cs
│   │   │   ├── LoginHandler.cs
│   │   │   ├── Dtos/
│   │   │   │   ├── RegisterUserDto.cs
│   │   │   │   ├── LoginDto.cs
│   │   │   │   └── AuthResultDto.cs
│   │   │   └── Validators/
│   │   │       ├── RegisterUserValidator.cs
│   │   │       └── LoginValidator.cs
│   │   ├── Users/
│   │   │   ├── GetUsersHandler.cs       # Admin paged list
│   │   │   ├── GetUserHandler.cs
│   │   │   ├── CreateUserHandler.cs
│   │   │   ├── UpdateUserHandler.cs
│   │   │   ├── DeleteUserHandler.cs     # Soft delete
│   │   │   ├── GetTeamHandler.cs        # Manager: scoped to managed users only
│   │   │   ├── Dtos/
│   │   │   └── Validators/
│   │   ├── Courses/
│   │   ├── Lessons/
│   │   ├── Enrollments/
│   │   ├── Progress/
│   │   ├── Quizzes/
│   │   ├── Skills/
│   │   ├── LearningPaths/
│   │   ├── Common/
│   │   │   └── PagedResult.cs
│   │   ├── Interfaces/
│   │   │   ├── ILmsDbContext.cs         # EF Core abstraction for testability
│   │   │   ├── IAuthService.cs          # Auth seam — local now, Entra ID later
│   │   │   └── ICurrentUserService.cs   # Resolves userId, role from HttpContext
│   │   └── LMS.Application.csproj
│   ├── LMS.Infrastructure/              # EF Core, Identity, SQL Server, JWT, Mapperly
│   │   ├── Persistence/
│   │   │   ├── LmsDbContext.cs
│   │   │   ├── Configurations/          # IEntityTypeConfiguration<T> per entity
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   └── ...
│   │   │   ├── Migrations/
│   │   │   └── SeedData.cs
│   │   ├── Auth/
│   │   │   ├── LocalAuthService.cs      # Implements IAuthService via Identity
│   │   │   ├── JwtService.cs
│   │   │   └── CurrentUserService.cs
│   │   ├── Mappers/                     # Mapperly source-generated mapper classes
│   │   │   ├── UserMapper.cs
│   │   │   └── ...
│   │   └── LMS.Infrastructure.csproj
│   └── LMS.Api/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── UsersController.cs
│       │   ├── CoursesController.cs
│       │   ├── LessonsController.cs
│       │   ├── EnrollmentsController.cs
│       │   ├── ProgressController.cs
│       │   └── QuizzesController.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json   # Docker connection string — committed (dev only)
│       ├── Program.cs
│       └── LMS.Api.csproj
├── tests/
│   ├── LMS.UnitTests/
│   │   ├── Validators/
│   │   │   ├── RegisterUserValidatorTests.cs
│   │   │   └── LoginValidatorTests.cs
│   │   └── LMS.UnitTests.csproj
│   └── LMS.IntegrationTests/
│       ├── Auth/
│       │   └── AuthEndpointTests.cs
│       ├── Users/
│       │   └── UserEndpointTests.cs      # Includes Manager scope test
│       ├── Enrollments/
│       │   └── EnrollmentEndpointTests.cs # State machine test
│       ├── Courses/
│       ├── Progress/
│       ├── Infrastructure/
│       │   ├── LmsWebApplicationFactory.cs
│       │   └── TestAuthHandler.cs         # Short-circuits JWT; injects user/role via header
│       └── LMS.IntegrationTests.csproj
└── frontend/
    └── lms-ui/                             # ng new lms-ui --standalone --routing --style=scss
        └── src/app/
            ├── core/
            │   ├── auth/
            │   │   ├── auth.service.ts
            │   │   ├── auth.guard.ts       # Functional guard using inject()
            │   │   └── jwt.interceptor.ts
            │   ├── models/                 # TypeScript interfaces mirroring DTOs
            │   └── services/              # Per-domain HTTP services
            ├── shared/
            │   └── components/
            └── features/                  # One folder per domain area — lazy loaded
                ├── auth/                  # login / register
                ├── courses/               # catalog, detail, lesson view
                ├── users/                 # admin user table (MatTable + sort/filter)
                ├── enrollments/           # approval flow (Manager)
                ├── progress/
                ├── quizzes/
                └── learning-paths/        # stretch
```

## Week 1 — Phased Build Plan (7 evenings × 2–3 hours)

### Evening 1 (~3h) — Auth Slice + Solution Setup

Goal: running backend with JWT auth, four roles, integration tests green.

- [ ] Plan-phase files reviewed and committed
- [ ] `dotnet new sln` + four projects (Domain, Application, Infrastructure, Api)
- [ ] Two test projects (UnitTests, IntegrationTests)
- [ ] Domain entities: User, UserRole enum
- [ ] EF Core + Identity; LmsDbContext with global query filter for soft delete
- [ ] Initial migration
- [ ] POST /api/auth/register, POST /api/auth/login
- [ ] JWT generation — roles claim included (must call `GetRolesAsync` explicitly)
- [ ] `IAuthService` interface defined (Entra ID seam)
- [ ] Integration tests: register, login, wrong password, role in token, duplicate email
- [ ] Unit tests: RegisterUserValidator, LoginValidator

### Evening 2 (~3h) — CI + User Management Backend

Goal: green CI running in GitHub Actions; user management API with scoped queries.

- [ ] Full `.github/workflows/ci.yml` committed, CI running
- [ ] GitHub remote created, status badge in README
- [ ] GET /api/users (Admin paged), GET /api/users/{id}, POST, PUT, DELETE (soft)
- [ ] GET /api/users/me (own profile)
- [ ] GET /api/users/my-team (Manager — scoped to their managed users only)
- [ ] Integration tests: Admin sees all, Manager sees only own team, 403 for others

### Evening 3 (~3h) — Angular Scaffolding + Auth UI + User Management UI + Course Catalog API

Goal: Angular app running; auth working end-to-end; user table; course API.

- [ ] `ng new lms-ui --standalone --routing --style=scss`
- [ ] Angular Material M3 theme setup
- [ ] JWT interceptor, auth guard, auth.service.ts
- [ ] Login and Register components (Reactive Forms, validation display)
- [ ] Admin user list (MatTable with sort/filter)
- [ ] GET /api/courses, GET /api/courses/{id}, POST, PUT, DELETE (Trainer/Admin)
- [ ] Skill tags on courses
- [ ] Integration tests: course CRUD, access control by role

### Evening 4 (~3h) — Course Catalog UI + Lessons

Goal: courses browsable; lessons readable; role-conditional UI.

- [ ] Course list and detail Angular components
- [ ] Lesson CRUD endpoints (nested under /api/courses/{id}/lessons)
- [ ] Lesson view component (render markdown — use ngx-markdown or marked)
- [ ] Role-conditional rendering: Trainer sees edit controls, Learner sees published only
- [ ] Tests: lesson ordering, access control, unpublished not visible to Learners

### Evening 5 (~3h) — Enrollment with Approval Workflow

Goal: full approval state machine working; integration tested.

- [ ] POST /api/enrollments (Learner request)
- [ ] GET /api/enrollments/pending (Manager — own team only)
- [ ] PUT /api/enrollments/{id}/approve, /reject
- [ ] Double-enrollment guard (unique constraint + handler check)
- [ ] Enrollment request UI (Learner requests from course detail page)
- [ ] Approval UI (Manager list with approve/reject actions)
- [ ] Integration tests: full state machine, cross-team access denied, double-enroll rejected

### Evening 6 (~3h) — Progress Tracking + Quizzes

Goal: progress bar visible; quiz can be taken; score saved.

- [ ] POST /api/lessons/{id}/complete (upsert — idempotent)
- [ ] GET /api/courses/{id}/progress (returns % complete)
- [ ] Skill acquisition on 100% completion
- [ ] Quiz endpoints: GET /api/lessons/{id}/quiz, POST attempt, score calculation
- [ ] Progress bar component (Material progress bar)
- [ ] Quiz component (Reactive Form, radio buttons per question)
- [ ] Tests: progress %, idempotent completion, quiz scoring

### Evening 7 (buffer / stretch — only if 1–6 are solid and tested)

- [ ] Learning Paths endpoints + UI (prerequisites logic)
- [ ] Skill profile endpoint + profile page
- [ ] Role-based dashboard components
- [ ] Final polish and documentation catch-up

## Week 2 — Azure Migration Plan

All Azure services used are free tier. No credit card charges for normal use.

### Step 1 — Azure SQL Database

1. Provision Azure SQL Server + Database (serverless, free offer: 32 GB)
2. Update connection string in GitHub Actions secret (environment variable injection)
3. Run EF Core migrations via `dotnet ef database update` in CI deploy job
4. Verify app starts and seed data runs

### Step 2 — Azure Key Vault

1. Create Key Vault (free tier: 10k ops/month)
2. Store connection string + JWT signing key as secrets
3. Add `Azure.Extensions.AspNetCore.Configuration.Secrets` NuGet package
4. Update `Program.cs` to read from Key Vault in production (environment variable: `AZURE_KEY_VAULT_URI`)
5. Grant App Service managed identity read access to Key Vault

### Step 3 — Azure App Service (API)

1. Create App Service Plan (F1 free tier) + Web App
2. Enable system-assigned managed identity (for Key Vault access — no stored credentials)
3. GitHub Actions deploy job: OIDC login → `dotnet publish` → deploy to App Service
4. Set `ASPNETCORE_ENVIRONMENT=Production` in App Service config
5. Verify Swagger UI accessible at the App Service URL

### Step 4 — Azure Static Web Apps (Angular)

1. Create Static Web App (free tier) linked to the GitHub repo
2. Azure auto-generates a GitHub Actions deploy workflow
3. Configure `environment.prod.ts` to point to the App Service URL
4. Verify Angular app loads and calls the API correctly

### Step 5 — OIDC Setup (no stored Azure secrets in GitHub)

1. Create App Registration in Entra ID
2. Add federated credential for GitHub Actions (`repo:owner/lms:environment:production`)
3. Store three non-secret values in GitHub secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
4. Use `azure/login@v2` in deploy job — no client secret needed

### Step 6 — Entra ID OAuth (stretch, free tier)

1. Update the App Registration to expose `/api` permissions
2. Implement `EntraIdAuthHandler` implementing `IAuthService`
3. Register it in `Program.cs` alongside `LocalAuthService`
4. Update Angular `auth.service.ts` to redirect to Entra ID login

## API Endpoint Reference

### Auth
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | /api/auth/register | None | Register; returns JWT |
| POST | /api/auth/login | None | Login; returns JWT |

### Users
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/users | Admin | Paged list of all users |
| GET | /api/users/{id} | Admin, Manager (own team) | Get user by ID |
| POST | /api/users | Admin | Create user with role |
| PUT | /api/users/{id} | Admin | Update user |
| DELETE | /api/users/{id} | Admin | Soft-delete (sets IsDeleted = true) |
| GET | /api/users/me | Any authenticated | Own profile |
| PUT | /api/users/me | Any authenticated | Update own profile |
| GET | /api/users/my-team | Manager | Managed users only |

### Courses
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/courses | Any authenticated | Published (Learner/Manager); all (Trainer/Admin) |
| GET | /api/courses/{id} | Any authenticated | Course detail |
| POST | /api/courses | Trainer, Admin | Create course |
| PUT | /api/courses/{id} | Trainer (owner), Admin | Update |
| DELETE | /api/courses/{id} | Trainer (owner), Admin | Soft-delete |

### Lessons
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/courses/{cId}/lessons | Enrolled, Trainer, Admin | List lessons |
| GET | /api/courses/{cId}/lessons/{id} | Enrolled, Trainer, Admin | Lesson detail |
| POST | /api/courses/{cId}/lessons | Trainer (owner), Admin | Create lesson |
| PUT | /api/courses/{cId}/lessons/{id} | Trainer (owner), Admin | Update |
| DELETE | /api/courses/{cId}/lessons/{id} | Trainer (owner), Admin | Delete |

### Enrollments
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/enrollments | Learner (own); Admin (all) | List enrollments |
| POST | /api/enrollments | Learner | Request enrollment |
| GET | /api/enrollments/pending | Manager | Team's pending requests |
| PUT | /api/enrollments/{id}/approve | Manager (own team) | Approve enrollment |
| PUT | /api/enrollments/{id}/reject | Manager (own team) | Reject enrollment |

### Progress
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/courses/{id}/progress | Learner (own), Trainer, Admin | Progress summary |
| POST | /api/lessons/{id}/complete | Learner (enrolled) | Mark lesson complete (idempotent) |
| GET | /api/manager/team-progress | Manager | Team progress overview |

### Quizzes
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/lessons/{id}/quiz | Enrolled, Trainer, Admin | Get quiz for lesson |
| POST | /api/lessons/{id}/quiz | Trainer (owner), Admin | Create quiz with questions |
| POST | /api/lessons/{id}/quiz/attempt | Learner (enrolled) | Submit answers; returns score |
| GET | /api/lessons/{id}/quiz/attempts | Learner (own) | My quiz attempts |

### Skills (stretch)
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/skills | Any authenticated | List all skills |
| POST | /api/skills | Admin | Create skill |
| GET | /api/users/{id}/skills | Own, Admin, Manager (team) | Acquired skills |

### Learning Paths (stretch)
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | /api/learningpaths | Any authenticated | List all |
| GET | /api/learningpaths/{id} | Any authenticated | Detail with courses |
| POST | /api/learningpaths | Admin, Trainer | Create |
| PUT | /api/learningpaths/{id} | Admin, Trainer | Update |

## Angular Route Map

```
/                          → redirect to /courses
/auth/login                → LoginComponent
/auth/register             → RegisterComponent
/courses                   → CourseListComponent         (lazy)
/courses/:id               → CourseDetailComponent       (lazy)
/courses/:id/lessons/:lid  → LessonViewComponent          (lazy)
/admin                     → AdminShellComponent          (lazy, guard: Admin)
  /admin/users             → UserListComponent            (MatTable, sort/filter)
  /admin/users/:id         → UserDetailComponent
/manager                   → ManagerShellComponent        (lazy, guard: Manager)
  /manager/team            → TeamOverviewComponent
  /manager/enrollments     → EnrollmentApprovalComponent
/profile                   → ProfileComponent             (lazy)
/learning-paths            → LearningPathListComponent    (lazy, stretch)
/learning-paths/:id        → LearningPathDetailComponent  (stretch)
```

## GitHub Actions CI Outline

Full `ci.yml` written after Auth slice. Template written in plan phase.

Trigger: push + PR to `main`, push to `feature/**`

**Backend job** (ubuntu-latest):
- `actions/checkout@v4`
- `actions/setup-dotnet@v4` (dotnet-version: `9.0.x`)
- `actions/cache@v4` (path: `~/.nuget/packages`, key: `runner.os + hashFiles('**/*.csproj')`)
- `dotnet restore`
- `dotnet build --no-restore --configuration Release`
- `dotnet test tests/LMS.UnitTests`
- `dotnet test tests/LMS.IntegrationTests`

**Frontend job** (ubuntu-latest, working-directory: `frontend/lms-ui`):
- `actions/checkout@v4`
- `actions/setup-node@v4` (node-version: `20`, cache: `npm`)
- `npm ci`
- `npm run build`
- `ng test --watch=false --browsers=ChromeHeadless`

**Week 2 CD additions** (separate `deploy.yml`, triggered on push to `main` after CI passes):
- `azure/login@v2` with OIDC (no stored client secret)
- Deploy API: `dotnet publish` → `azure/webapps-deploy@v3`
- Deploy Angular: push to Static Web Apps via `Azure/static-web-apps-deploy@v1`

## Testing Strategy Summary

| Layer | Tool | DB | Scope |
|-------|------|----|-------|
| API integration | xUnit + WebApplicationFactory | SQLite in-memory | Auth flows, role access, state machines |
| App unit tests | xUnit + FluentAssertions | None | Validators, business rules |
| Angular services | Jasmine + HttpTestingController | N/A | All service HTTP methods |
| Angular components | Jasmine + TestBed | N/A | Forms, role-conditional logic only |
| E2E | None (deferred) | N/A | Out of scope for week 1 |

Critical paths requiring integration test coverage:
1. Auth: register → login → JWT has role claim → authorized request succeeds
2. Role-based access: each role tested on each protected endpoint (200 vs 403)
3. Manager scope: cannot see or act on another manager's team
4. Enrollment state machine: Pending→Approved, Pending→Rejected, double-enroll denied
5. Progress: % calculation correct; idempotent lesson completion
6. Quiz: score calculation; only enrolled Learner can attempt
