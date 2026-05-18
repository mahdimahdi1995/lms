# Project Context — Enterprise LMS

## What This Is

A full-stack enterprise Learning Management System (LMS) built from scratch as a portfolio
project, demonstrating end-to-end proficiency with the .NET + Angular enterprise stack.

B2B enterprise training platforms require a specific set of concerns that most simple LMS
tutorials skip: multi-role access control, manager-gated enrollment workflows, approval
state machines, clean architecture with testable seams, and CI that is green from day one.
This project covers all of them.

## Tech Stack Rationale

| Choice | Reason |
|--------|--------|
| ASP.NET 9 Web API | Modern, minimal, high-performance .NET |
| EF Core 9 | Proven ORM; global query filters, migrations, excellent testing support |
| ASP.NET Core Identity + JWT | Extensible auth; Entra ID seam designed in from the start |
| Mapperly | Source-generated mapping; compile-time errors; zero runtime reflection overhead |
| FluentValidation | Expressive, testable validators; standard in .NET enterprise |
| Angular (latest) + Material | Standalone components, signals, M3 theming; modern SPA stack |
| SQL Server in Docker | Enterprise-standard RDBMS without a local installation |
| xUnit + WebApplicationFactory | Fast, reliable integration testing without Testcontainers overhead |
| GitHub Actions | First-class CI; OIDC-ready patterns |

## Key Decisions

- **No MediatR** — became commercial in 2024; plain handler classes are cleaner at this scale
- **Mapperly over AutoMapper** — AutoMapper went commercial in 2025; Mapperly is source-generated and free
- **Local auth first, Entra ID seam built in** — `IAuthService` is provider-agnostic; adding OAuth is a config + handler addition, not a rewrite
- **Test-after-the-slice** — more productive than TDD when learning framework patterns; tests lock in each slice before moving on
- **Soft delete** — never hard-delete users; GDPR right-to-erasure via PII anonymisation, not physical deletion
- **WebApplicationFactory + SQLite in-memory** — fast, dependency-free CI; Testcontainers is noted as the production-grade upgrade path

## How to Resume Work in a New Session

1. Read [PLAN.md](PLAN.md) — full solution structure, phased build plan, API endpoints, Angular route map
2. Read [ARCHITECTURE.md](ARCHITECTURE.md) — layered structure, all key seams (auth, multi-tenancy, soft delete), Angular patterns
3. Read [DOMAIN.md](DOMAIN.md) — entity descriptions and German↔English glossary
4. Read [NOTES.md](NOTES.md) — gotchas, concepts to review, key technical decisions
5. Run `git log --oneline` to see what has been built
6. Run `dotnet test` and `ng test --watch=false` to check current state
