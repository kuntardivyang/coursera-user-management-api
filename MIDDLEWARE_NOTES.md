# Middleware Pipeline — Activity 3 Deliverable

This document describes the three custom middleware components added in
activity 3, the pipeline order, and how to verify each piece.

## Components

### `ErrorHandlingMiddleware`

`Middleware/ErrorHandlingMiddleware.cs`

- Wraps the rest of the pipeline in `try` / `catch`.
- On any unhandled exception:
  - Logs `LogError` with the exception, HTTP method, and path.
  - If the response has not started, clears it and returns
    `500 Internal Server Error` with body `{"error": "Internal server error."}`
    and `Content-Type: application/json`.
  - If the response has already started (headers flushed), rethrows — there
    is nothing safe to overwrite.
- Replaces the activity-2 `IExceptionHandler` approach. The behavior is
  equivalent for our needs and fits the activity brief literally.

### `RequestLoggingMiddleware`

`Middleware/RequestLoggingMiddleware.cs`

- Logs **method** + **path** (with query string) at request start.
- Times the downstream pipeline with `Stopwatch`.
- Logs **method** + **path** + **status code** + **elapsed ms** at the end,
  inside a `finally` so timings still log when downstream throws.

### `TokenAuthenticationMiddleware`

`Middleware/TokenAuthenticationMiddleware.cs`

- Reads the expected token from `Auth:ApiToken` (config / env var).
- Allows any path under `/swagger` or `/openapi` through without a token so
  the API doc remains usable in Development. Everything else is gated.
- Requires `Authorization: Bearer <token>`. Rejections:
  - Missing header → `401 {"error":"Unauthorized."}`
  - Non-Bearer scheme → `401 {"error":"Unauthorized."}`
  - Wrong / empty token → `401 {"error":"Unauthorized."}`
- Sets `WWW-Authenticate: Bearer` on rejection per RFC 6750.
- Logs every rejection at `Warning` so failed-auth attempts remain visible
  even though `RequestLoggingMiddleware` runs after it (see ordering note).

## Pipeline order

`Program.cs` registers the middleware in the order the activity specifies:

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();        // 1. outermost
app.UseMiddleware<TokenAuthenticationMiddleware>();  // 2. gate
app.UseMiddleware<RequestLoggingMiddleware>();       // 3. innermost
```

Why this order:

- **Error handling first.** If anything below — including the auth
  middleware itself — throws, this middleware turns it into a clean JSON
  500. Putting it later would let exceptions escape into Kestrel's default
  handler.
- **Authentication next.** Once we know the request will be handled, we
  reject unauthenticated traffic before it touches the endpoint.
- **Logging last.** The timing wraps just the endpoint work, so the elapsed
  ms reflects the cost of the actual user code rather than auth overhead.

### Trade-off note

Placing logging *innermost* means failed-auth requests do **not** flow
through `RequestLoggingMiddleware`. They are still recorded — the auth
middleware itself logs every rejection at `Warning` level — but if the
auditing requirement is "every request, no exceptions", swap the order so
logging wraps auth:

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();   // now wraps auth too
app.UseMiddleware<TokenAuthenticationMiddleware>();
```

I followed the activity brief verbatim and noted the alternative here.

## Configuration

`appsettings.json` declares an `Auth:ApiToken` placeholder that **must** be
overridden in production (env var `Auth__ApiToken` is the standard way for
container deployments). `appsettings.Development.json` ships a sample dev
token (`dev-techhive-token-12345`) so the project runs out-of-the-box for
local testing.

## How to verify

`UserManagementAPI.http` includes a "middleware" section that exercises
each piece. A summary:

| Case                                          | Expected                                             |
| --------------------------------------------- | ---------------------------------------------------- |
| `GET /api/users` no header                    | `401 {"error":"Unauthorized."}` + `WWW-Authenticate` |
| `GET /api/users` with bad token               | `401 {"error":"Unauthorized."}`                      |
| `GET /api/users` with `Basic` scheme          | `401 {"error":"Unauthorized."}`                      |
| `GET /api/users` with valid token             | `200 PagedResult<User>`                              |
| `GET /swagger/v1/swagger.json` (no token)     | `200` (public path)                                  |
| `GET /api/diagnostics/throw` with valid token | `500 {"error":"Internal server error."}`             |

When the server is running you should see paired log lines for each
authenticated request, e.g.:

```
info: HTTP GET /api/users started
info: HTTP GET /api/users responded 200 in 4ms
```

And for a forced exception:

```
info: HTTP GET /api/diagnostics/throw started
info: HTTP GET /api/diagnostics/throw responded 200 in 9ms
fail: Unhandled exception on GET /api/diagnostics/throw
      System.InvalidOperationException: Intentional test exception ...
```

> **Caveat — observed during live testing.** With logging registered
> innermost (per the activity brief), the logging `finally` runs *before*
> the error middleware's `catch`. By that point the controller has thrown
> but no status code has been written, so `context.Response.StatusCode`
> still reads `200`. The client correctly receives `500` from the error
> middleware, but the request log line shows `200`. Inverting the order
> (logging outermost: `error → logging → auth`) would let logging observe
> the final status set by the error handler. Either swap the registrations
> or add an explicit `try/catch` inside `RequestLoggingMiddleware` if you
> need accurate status logging on exception paths.

## How Copilot helped

- **Drafted all three middleware classes from one-line prompts.** The
  convention-based shape (`(RequestDelegate next, …)` constructor +
  `InvokeAsync(HttpContext)` method) was suggested directly so I did not
  have to remember the exact signature.
- **Recommended `context.Response.HasStarted` guard** in
  `ErrorHandlingMiddleware`. Easy to forget; without it, attempting to
  rewrite an already-flushed response throws a confusing
  `InvalidOperationException` *during* error handling.
- **Suggested the `WWW-Authenticate: Bearer` header on 401**. Copilot
  flagged it as an RFC 6750 expectation — useful when downstream clients
  do conditional retries based on the challenge.
- **Suggested allow-listing `/swagger`**. First draft of the auth
  middleware blocked Swagger UI, making local dev painful. Copilot
  recommended the prefix allow-list pattern over hard-coding paths.
- **Suggested `IConfiguration["Auth:ApiToken"]`** over a hard-coded string,
  with an explicit throw at construction if missing — fail-fast at app
  start rather than handing out 401s for the wrong reason.
- **Recommended `Stopwatch` over `DateTime.UtcNow` subtraction** for
  request timing (monotonic clock; not affected by NTP adjustments).
- **Wrote the Swagger `AddSecurityDefinition` / `AddSecurityRequirement`
  block** so the Swagger UI shows an "Authorize" button accepting the
  Bearer token — testing endpoints in the browser Just Works.
- **Drafted the `.http` test matrix** covering missing / wrong-scheme /
  wrong-token / valid-token / forced-exception cases.

## What I changed by hand after Copilot's draft

- Copilot's first cut at `TokenAuthenticationMiddleware` compared the token
  with `string.Equals(..., OrdinalIgnoreCase)`. Tightened to ordinal
  case-sensitive (tokens are opaque secrets — case matters).
- Copilot proposed embedding the token in `Program.cs` as a const for
  brevity. Replaced with config lookup so the dev / prod token can differ
  and prod can use environment overrides.
- Copilot wrote the logging middleware with `Console.WriteLine`. Replaced
  with `ILogger<RequestLoggingMiddleware>` so output respects the standard
  log-level configuration and structured logging works in production.
- Copilot put logging *first* in the pipeline (the conventional best
  practice). Reordered to match the activity's stated requirement
  (error → auth → logging) and documented the trade-off above.
- Copilot drafted the diagnostics endpoint without the `IsDevelopment`
  guard. Added it so `GET /api/diagnostics/throw` returns 404 outside
  Dev — production never gets a forced-error endpoint.
