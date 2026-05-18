# Testing Philosophy

## Why Test-After-the-Slice (Not TDD)

TDD works best when the API design is stable and well-understood before you start coding.
When learning a new framework under time pressure, you want to build the feature, understand
how the framework actually wires things together, then write tests that lock it in. Writing
tests before the implementation in an unfamiliar framework often means rewriting the tests
halfway through when you discover how things actually work.

We also rely heavily on AI assistance, which can introduce subtle bugs. Tests are the feedback
loop that catches regressions before they compound across slices.

**The rule**: a slice is not done until its tests pass in CI. Tests are not optional cleanup
at the end — they are the last step of each slice, alongside documentation updates.

---

## Test Priorities

### 1. API Integration Tests — Highest Value

**What they cover**: a full HTTP request travels through the middleware pipeline, hits the
controller, calls the handler, touches the database, and returns a response. The most code
is exercised per test.

**How**: `WebApplicationFactory<Program>` with EF Core configured to use SQLite in-memory
instead of SQL Server. A custom `TestAuthHandler` short-circuits JWT validation and injects
a specified user ID and role via request headers, so tests can act as any role without
generating real tokens.

**What must have integration tests**:

| Flow | What to verify |
|------|----------------|
| Auth | Register → login → JWT contains role claim → authorized request succeeds |
| Role-based access | Each protected endpoint returns 200 for the allowed role and 403 for others |
| Manager team scope | A Manager cannot see or act on another manager's team members |
| Enrollment state machine | Pending → Approved, Pending → Rejected, double-enroll rejected |
| Progress | % calculation is correct; marking a lesson complete twice does not create duplicates |
| Quiz | Score is calculated correctly; only an enrolled Learner can submit an attempt |

### 2. Application Layer Unit Tests

**What they cover**: FluentValidation validators and handler business rules in isolation.
No HTTP stack, no database.

**What to test**:
- All FluentValidation validators: empty fields, invalid email, password too short, etc.
- Handler edge cases that would be cumbersome to test end-to-end, e.g.: what happens when a
  Manager tries to approve an enrollment that belongs to another Manager's team?

**Characteristics**: fast (milliseconds), precise, no setup overhead.

### 3. Angular Service Tests

**What they cover**: each HTTP service method sends the correct URL, method, headers, and body.

**How**: Jasmine + `HttpTestingController` from `@angular/common/http/testing`.
`HttpTestingController.expectOne('/api/courses')` intercepts the call, verifies it, and
returns a mock response synchronously.

**What to test**: every public method on every service that makes an HTTP call. These are
quick to write and catch URL typos and missing auth header attachment before they reach
manual testing.

### 4. Angular Component Tests — Selective

**What they cover**: components with real logic — not every component.

**Write a component test when**:
- The component has a Reactive Form with validation display logic
- The component shows or hides elements conditionally based on the user's role
- The component manages meaningful signal state that a user interacts with

**Skip a component test when**:
- The component only binds data to the template with no conditional logic
- Dumb display components (`<p>{{ course.title }}</p>`) need no test

---

## What We Deliberately Skip

### E2E Tests (Playwright / Cypress)
Not included. Setup overhead is too high for a focused build. One brittle E2E test
that fails intermittently is worse than none — it erodes trust in the test suite.

The right answer when asked: *"I would add E2E tests for critical user journeys in a
production project — login, enrollment request, quiz submission. For this project I
prioritised API integration tests, which give higher signal per hour and are less fragile."*

### Coverage Percentage Targets
No coverage target is set. Meaningful coverage of the critical paths listed above is the
goal. A thorough integration test for the enrollment state machine is worth more than
ten tests checking that a getter returns its field value.

---

## Test Infrastructure

### Backend

**`LmsWebApplicationFactory`** extends `WebApplicationFactory<Program>`. It:
- Replaces the SQL Server EF Core provider with SQLite in-memory
- Registers `TestAuthHandler` as the JWT bearer handler (short-circuits real validation)
- Optionally skips seed data so tests start with a clean database

**`TestAuthHandler`** reads `X-Test-UserId` and `X-Test-Role` request headers and constructs
a `ClaimsPrincipal` from them. Any test can act as any user with any role by setting two
headers — no real JWT generation or validation involved.

**Database isolation**: each `WebApplicationFactory` instance creates a uniquely-named
in-memory database. Test classes that need a clean slate reset the database using
`DbContext.Database.EnsureDeleted()` followed by `EnsureCreated()`.

### Frontend

**`HttpClientTestingModule`** replaces `HttpClient` with a test double. After calling the
service method, use `HttpTestingController.expectOne(url)` to assert the request was made,
then call `.flush(mockData)` to return a response synchronously:

```typescript
it('should fetch courses', () => {
  service.getAll().subscribe(courses => expect(courses.length).toBe(2));

  const req = httpMock.expectOne('/api/courses');
  expect(req.request.method).toBe('GET');
  req.flush([{ id: '1', title: 'Course A' }, { id: '2', title: 'Course B' }]);
});
```

---

## Testcontainers — Why It Is Not Used Here

In a production codebase, **Testcontainers** spins up a real SQL Server Docker container per
test run, giving full dialect fidelity — no SQLite workarounds. The trade-off is approximately
30 seconds of container startup time in CI versus under 1 second for in-memory SQLite.

For this project, in-memory is the right call. In a production team setting with CI running
dozens of times per day, Testcontainers is the correct choice. Mentioning this trade-off
demonstrates awareness of both options.
