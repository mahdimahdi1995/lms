# Notes — Decisions, Gotchas, and Concepts to Review

## Key Technical Decisions

### Mapperly over AutoMapper
Mapperly is source-generated that the compiler writes the mapping code at build time, so there
is zero runtime reflection and zero overhead. Mapping errors are compile-time, not runtime
surprises. AutoMapper moved to a commercial licence in 2025, making Mapperly the natural
free replacement in modern .NET projects.

### No MediatR
Plain handler classes are used instead: `RegisterUserHandler`, `LoginHandler`, etc. MediatR
changed to a commercial licence in 2024. For a project of this size it also adds indirection
without much benefit. MediatR is worth it when you need cross-cutting pipeline behaviours
(`IPipelineBehavior`) applied to dozens of commands. We handle logging and validation
explicitly here, which keeps the code more readable.

### Local auth first, Entra ID seam built in
`IAuthService` in Application is provider-agnostic. `LocalAuthService` in Infrastructure
implements it using ASP.NET Core Identity for week 1. Adding Entra ID in week 2 means
implementing one new class and registering it and no business logic changes.

### WebApplicationFactory + SQLite in-memory for integration tests
Testcontainers is the production-grade choice (real SQL Server, full dialect fidelity).
For this project, SQLite in-memory is fast, dependency-free, and CI-friendly. Trade-off:
SQLite has dialect differences (no `DATALENGTH`, limited transaction semantics). We avoid
SQL Server-specific raw SQL in tested code; EF Core's provider abstraction handles the rest.

### Test-after-the-slice, not TDD
TDD requires knowing the API before writing the implementation — difficult when learning a
framework under time pressure. We build the feature first, understand how it works, then lock
it in with tests. Each slice is not considered done until its tests pass in CI.

### Soft delete for users
We never hard-delete user records. `IsDeleted = true` hides them from all EF Core queries
via a global query filter. GDPR right-to-erasure is handled by anonymising PII fields (null
out Email, FirstName, LastName) — physical deletion would cascade-break enrollment history
and audit trail records that are legally significant in many jurisdictions.

### appsettings.Development.json vs User Secrets
`appsettings.Development.json` is committed with the Docker SQL Server password. This is
acceptable because it is a dev-only credential with no production value. For the JWT signing
key, use `dotnet user-secrets` — the secrets store lives outside the repository. In
production (Azure), both come from Azure Key Vault via the managed identity.

---

## Angular Concepts to Review

### Signals vs Observables — when to use which

**Signals** (`signal()`, `computed()`, `effect()`) are synchronous reactive state. Use them for:
- Component-local UI state (is the menu open? which tab is active?)
- Derived values from other signals (`computed(() => items().length)`)
- Simple state that does not involve async operations

**Observables (RxJS)** are async streams. Use them for:
- HTTP calls (Angular's HttpClient returns Observables)
- WebSocket events, router events, form value changes
- Complex transformations: `switchMap`, `combineLatest`, `debounceTime`, `distinctUntilChanged`

**Boundary pattern**: keep Observables in services; convert to signals at the component
boundary with `toSignal(obs, { initialValue: [] })`. This avoids the `async` pipe in
templates and gives you synchronous access in `@if` and `@for` blocks.

```typescript
// In the component
readonly courses = toSignal(this.coursesService.getAll(), { initialValue: [] });

// In the template — no async pipe needed
@for (course of courses(); track course.id) { ... }
```

### inject() vs constructor injection
`inject()` is preferred in modern Angular — especially in functional guards, HTTP interceptors,
and standalone components. It works in any injection context, not just constructors. Constructor
injection still works; both are valid. Be consistent within a file.

```typescript
// Functional guard — inject() is the only option here
export const adminGuard = (): CanActivateFn => () => {
  const auth = inject(AuthService);
  return auth.isAdmin();
};
```

### New control flow — @if, @for, @switch
Angular 17+ built-in template syntax. Use everywhere — do not use `*ngIf` or `*ngFor` in
new code.

```html
@if (isAdmin()) {
  <admin-panel />
} @else {
  <p>Access denied.</p>
}

@for (course of courses(); track course.id) {
  <app-course-card [course]="course" />
} @empty {
  <p>No courses available.</p>
}
```

### Angular Material M3 Theming
Material 3 (M3) theming changed significantly in Angular Material 17+. Key points:
- Use `mat.define-theme()` and `mat.all-component-themes()` in `styles.scss`
- CSS custom properties / design tokens replace the old `.mat-*` class overrides
- Do not use deprecated `mat-color()` or `mat-palette()` — those are pre-M3
- Import each Material component explicitly in the standalone component's `imports: []` array;
  there is no single `MaterialModule` barrel in M3

### Reactive Forms — typed forms
Angular 14+ supports fully typed `FormGroup` and `FormControl`. Always type your forms:

```typescript
readonly form = new FormGroup({
  email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
});
```

With `nonNullable: true`, `.reset()` restores the initial value instead of setting `null`.

---

## EF Core Concepts to Review

- **Global query filters**: `HasQueryFilter(e => !e.IsDeleted)` — applies to all queries on that entity type automatically
- **`IgnoreQueryFilters()`**: used in admin endpoints to see soft-deleted records
- **`AsNoTracking()`**: use on read-only GET queries to skip change tracking overhead
- **N+1 problem**: always use `.Include()` / `.ThenInclude()` or projection queries for related data; never load collections inside a loop
- **Owned entities**: value objects that share the parent's table (e.g., an `Address` type)
- **`AsSplitQuery()`**: for queries loading multiple collections (avoids cartesian product in a single SQL join)

---

## ASP.NET Core Identity + JWT Concepts to Review

- `UserManager<ApplicationUser>` — `CreateAsync`, `FindByEmailAsync`, `CheckPasswordAsync`
- **Roles in JWT**: Identity does not add role claims to the JWT automatically. You must call
  `GetRolesAsync(user)` and add them manually:
  ```csharp
  var roles = await userManager.GetRolesAsync(user);
  var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
  ```
  Without this, `[Authorize(Roles = "Admin")]` never fires.
- **Policy-based auth** is more flexible than role-based auth for complex rules, e.g.:
  "CanApproveEnrollment" = role is Manager AND the enrollment belongs to one of their team members
- `ICurrentUserService` reads `UserId` and `Role` from `HttpContext.User.Claims` — injected
  into handlers to avoid threading HttpContext through every call

---

## GitHub Actions Concepts to Review

- **`actions/cache@v4`**: cache NuGet packages keyed by `hashFiles('**/*.csproj')` — invalidated whenever a project file changes
- **`actions/setup-node@v4`**: set `cache: 'npm'` with `cache-dependency-path` pointing to `package-lock.json` for fast npm installs
- **`needs:`**: makes one job wait for another — use so the CD deploy job only runs after CI passes
- **OIDC (federated credentials)**: GitHub Actions authenticates to Azure using a short-lived token tied to the repository, not a stored client secret. Configured once in Entra ID, then the workflow uses `azure/login@v2` with three non-secret values (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`).

---

## Gotchas

- **JWT role claims not automatic** — the single most common auth bug: forgot to call `GetRolesAsync` before building the token. Always verify with a test that decodes the JWT.
- **EF Core + SQLite in tests** — SQLite does not support all SQL Server types or functions. Avoid `decimal` precision columns greater than what SQLite supports, and do not write raw SQL using SQL Server-specific syntax in code that is exercised by integration tests.
- **CORS in development** — the API must allow `http://localhost:4200` or the Angular dev server calls will be blocked by the browser. Add `.AllowAnyOrigin()` (dev only) or a specific origin policy.
- **Angular Material imports in standalone components** — each component must list the Material modules it uses in its own `imports: []` array. There is no global import. Forgetting one gives a confusing "unknown element" error at runtime.
- **Soft delete + Identity tables** — `ApplicationUser` extends `IdentityUser`. Adding `IsDeleted` requires a custom EF Core entity configuration, not just a property on the class, because Identity manages its own table setup.
- **`toSignal()` requires injection context** — must be called inside a constructor, field initialiser, or another injection-context-aware function. Calling it inside `ngOnInit` will throw unless you use `runInInjectionContext`.