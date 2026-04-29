# Debugging Pass — How Copilot Helped

This document is the deliverable for activity 2 ("Debugging API Code with
Copilot"). It lists the bugs Copilot flagged in the activity-1 codebase, the
fixes applied, and how to reproduce / verify each.

## Bugs found and fixed

### 1. Whitespace-only string fields passed validation

**Symptom.** A `POST /api/users` body with `"firstName": "   "` returned
`201 Created` because `[Required]` only rejects null/empty and
`[StringLength(MinimumLength = 1)]` only enforces *length*, not content.

**Fix.** New `Validation/NotWhitespaceAttribute.cs` and applied it to every
non-email string field on `User`, `CreateUserRequest`, `UpdateUserRequest`.

**Repro / verify.** `UserManagementAPI.http` → "Whitespace-only first name"
request now returns `400 ValidationProblemDetails` with the message
*"The FirstName field cannot be blank or whitespace."*

### 2. Race condition on email uniqueness (POST/PUT)

**Symptom.** `Controller.Create` did `EmailExists()` then `Service.Create()`
as two separate calls. Two concurrent POSTs with the same email could both
pass the existence check and both succeed, breaking the uniqueness invariant.

**Fix.** Moved the check + write into a single locked critical section in
`InMemoryUserService.TryCreate` / `TryUpdate`. The controller now calls one
atomic operation and inspects the return value.

**Repro / verify.** Send two parallel `POST /api/users` requests with the
same email (e.g. with `hey -n 50 -c 25` or `xargs -P`). Exactly one returns
`201`, the rest `409 Conflict`. Pre-fix, multiple `201`s could land.

### 3. Unhandled exceptions returned bare 500s with no body

**Symptom.** Any throw outside of an explicit `try/catch` resulted in a
generic 500 response with either an empty body (Production) or a raw
stack trace (Development) — neither machine-parseable nor safe.

**Fix.** Added a global exception handler that logs the exception with
method + path and returns a structured JSON 500. The activity-2 cut used
`IExceptionHandler` + `ProblemDetails`; activity 3 superseded this with a
classic `ErrorHandlingMiddleware` returning `{"error":"Internal server error."}`
to match the brief literally. Behavior is equivalent for clients —
see `MIDDLEWARE_NOTES.md`.

**Repro / verify.** Temporarily throw inside any controller action — the
client receives a structured `ProblemDetails` 500, the server logs an
`LogError` entry. Removed before commit.

### 4. `GET /api/users` returned the entire collection (perf / DoS surface)

**Symptom.** As TechHive's user list grows, every list call serializes
every user. Bandwidth scales linearly with directory size and there is no
back-pressure mechanism.

**Fix.** Pagination on `GET /api/users` via `?page=` and `?pageSize=`
(defaults: page 1, size 20; cap: size 100). Response shape is now
`PagedResult<User>` with `page`, `pageSize`, `totalCount`, `totalPages`,
`items`. Snapshot of `_users.Values` is taken once per request so the page
slice and count agree under concurrent writes.

**Repro / verify.** `GET /api/users?page=1&pageSize=20` returns paged
envelope. `GET /api/users?page=0&pageSize=20` and `pageSize=999` both
return `400 ProblemDetails` with explicit messages.

### 5. Bad-shape ids returned 404 instead of 400

**Symptom.** `GET /api/users/-1` and `/api/users/0` returned `404 Not Found`,
which conflates "you asked for something that does not exist" with "your
request is malformed".

**Fix.** `id <= 0` → `400 ProblemDetails` ("id must be a positive integer.")
on GET-by-id, PUT, DELETE.

**Repro / verify.** `GET /api/users/-1` now `400`.

### 6. HTTPS redirect broke local Postman/curl over plain HTTP

**Symptom.** `app.UseHttpsRedirection()` ran in Development too, so calls
to `http://localhost:5080/api/users` redirected to `https://localhost:7080`
which fails without a trusted dev cert (typical fresh box).

**Fix.** `UseHttpsRedirection` is now Production-only. Development still
binds both http and https; tests over plain HTTP just work.

### 7. Inconsistent error response shape

**Symptom.** Some errors used `new { message = "..." }` (anonymous object),
others (validation 400 from `[ApiController]`) used
`ValidationProblemDetails`. Two shapes for client error handling.

**Fix.** All controller-emitted errors now use `ProblemDetails` via the
`Problem(...)` helpers. Validation 400s remain `ValidationProblemDetails`
(superset of ProblemDetails). Consistent, RFC 7807-compliant.

## Test matrix

The `UserManagementAPI.http` file now exercises the full edge-case set —
each case is tagged with the expected status. Importing the same routes
into Postman covers the activity's testing requirement.

| Case                              | Expected         |
| --------------------------------- | ---------------- |
| List users                        | `200` PagedResult |
| List with `page=0`                | `400` ProblemDetails |
| List with `pageSize=999`          | `400` ProblemDetails |
| Get user by id (existing)         | `200` User       |
| Get user `id=9999`                | `404` ProblemDetails |
| Get user `id=-1`                  | `400` ProblemDetails |
| Create with whitespace name       | `400` ValidationProblemDetails |
| Create with malformed email       | `400` ValidationProblemDetails |
| Create with duplicate email       | `409` ProblemDetails |
| Update non-existent id            | `404` ProblemDetails |
| Update with conflicting email     | `409` ProblemDetails |
| Delete non-existent id            | `404` ProblemDetails |

## How Copilot streamlined the work

- **Surfaced the whitespace gap.** Reading the model annotations, Copilot
  pointed out that `[StringLength(MinimumLength=1)]` only checks length and
  proposed a `NotWhitespaceAttribute` rather than a one-off `if` in each
  action.
- **Suggested `IExceptionHandler` over middleware boilerplate.** The
  classic answer is `app.Use(async (ctx, next) => { try { await next(); } catch ...})`.
  Copilot suggested the .NET 8 `IExceptionHandler` interface plus
  `AddProblemDetails()` — less code, and the handler can be unit-tested.
- **Caught the race condition.** When asked "review `Create` for concurrency
  issues", Copilot pointed out the TOCTOU between `EmailExists` and `Create`
  and recommended either a lock or upserting via `ConcurrentDictionary` with
  a secondary email index. Lock was simpler for the in-memory store; the
  `IUserService` shape (`TryCreate` / `TryUpdate` returning a tuple) lets
  the future EF Core implementation enforce uniqueness via a DB constraint
  with the same controller code.
- **Drafted pagination boilerplate.** `PagedResult<T>`, the `Skip/Take`
  slice, `TotalPages` computation, and the page-bounds validation in the
  controller — all generated by Copilot from a one-line prompt and lightly
  edited (added the `MaxPageSize = 100` cap and the snapshot-once detail
  to keep `count` and `items` consistent under concurrent writes).
- **Standardized error responses.** Copilot recommended replacing
  `new { message = ... }` with `Problem(...)` helpers so all errors are
  ProblemDetails — discovered by asking "what's the idiomatic way to return
  errors from an ASP.NET Core controller in 2024?".
- **Wrote the edge-case `.http` file.** Once the matrix was decided,
  Copilot expanded a few seed entries into the full set of negative-path
  requests in seconds.

## What I changed by hand after Copilot's draft

- Copilot's first cut at `IExceptionHandler` exposed `exception.ToString()`
  in the response detail. Tightened to `_env.IsDevelopment() ? Message : null`
  so production never leaks stack traces.
- Copilot's pagination defaulted to no upper bound on `pageSize`. Added the
  `MaxPageSize = 100` cap and the explicit `400` for out-of-range values.
- Copilot suggested catching exceptions per-action with `try/catch` blocks.
  Replaced with one global handler and documented the choice here — the
  activity asks for "try-catch blocks for unhandled exceptions" and the
  global handler is functionally a single try/catch that wraps the entire
  pipeline (which is the modern recommendation).
- Copilot proposed swallowing `DbException` specifically. Removed — there
  is no DB yet, and the global handler already covers it once we add EF.
