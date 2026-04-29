# UserManagementAPI

CRUD Web API for TechHive Solutions' internal HR & IT user records.
Built with ASP.NET Core 8 (controllers + Swagger), in-memory storage,
custom middleware for token authentication, request logging, and
consistent error responses.

> Coursera back-end capstone. Three activities are documented in
> [`COPILOT_NOTES.md`](./COPILOT_NOTES.md),
> [`DEBUGGING_NOTES.md`](./DEBUGGING_NOTES.md), and
> [`MIDDLEWARE_NOTES.md`](./MIDDLEWARE_NOTES.md).

## Prerequisites

- .NET SDK 8.0+ (`dotnet --version`)

If you do not have it yet:

```bash
# Ubuntu / Debian
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0
export PATH="$HOME/.dotnet:$PATH"
```

## Run

```bash
git clone https://github.com/kuntardivyang/coursera-user-management-api.git
cd coursera-user-management-api
dotnet restore
dotnet run --launch-profile http
```

Then open Swagger UI: <http://localhost:5080/swagger>

## Authentication

Every endpoint except `/swagger/*` requires an `Authorization: Bearer <token>`
header. The token is read from `Auth:ApiToken`:

- **Development**: pre-set to `dev-techhive-token-12345` in `appsettings.Development.json`.
- **Production**: must be supplied via env var `Auth__ApiToken` or a secrets store.
  The placeholder in `appsettings.json` is intentionally invalid.

Example:

```bash
curl -H "Authorization: Bearer dev-techhive-token-12345" \
     http://localhost:5080/api/users
```

In Swagger UI, click **Authorize** and paste the token to attach it to all
sample requests.

## Endpoints

| Method | Route                               | Purpose                            |
| ------ | ----------------------------------- | ---------------------------------- |
| GET    | `/api/users?page=1&pageSize=20`     | List users, paginated              |
| GET    | `/api/users/{id}`                   | Get a single user by id            |
| POST   | `/api/users`                        | Create a new user                  |
| PUT    | `/api/users/{id}`                   | Update an existing user            |
| DELETE | `/api/users/{id}`                   | Delete a user by id                |
| GET    | `/api/diagnostics/throw`            | Forced exception (Development only) |

`pageSize` is capped at 100. Defaults: `page=1`, `pageSize=20`. Response shape
for the list endpoint is `PagedResult<User>` (`page`, `pageSize`, `totalCount`,
`totalPages`, `items`).

Validation is enforced via data annotations (required, email format, length
limits, custom `NotWhitespace`). Status codes:

- `400 Bad Request` — invalid body, bad query params, non-positive id
- `401 Unauthorized` — missing / wrong-scheme / wrong token (`{"error":"Unauthorized."}`)
- `404 Not Found` — unknown user id
- `409 Conflict` — duplicate email (atomically enforced under concurrent writes)
- `500 Internal Server Error` — caught by `ErrorHandlingMiddleware` (`{"error":"Internal server error."}`)
- `201 Created` (with `Location` header) on successful POST
- `204 No Content` on successful DELETE

CRUD validation errors use RFC 7807 `ValidationProblemDetails` /
`ProblemDetails`; the auth and global-error middleware return the simpler
`{"error": "..."}` shape required by the activity brief.

## Testing with Postman / `.http`

The repo includes `UserManagementAPI.http` with sample requests for VS Code
REST Client / Rider, including auth-failure cases and the forced-exception
endpoint. For Postman, import the same routes manually or use the Swagger UI
to generate calls.

Example POST body:

```json
{
  "firstName": "Priya",
  "lastName": "Singh",
  "email": "priya.singh@techhive.example",
  "department": "HR",
  "role": "Recruiter"
}
```

## Project layout

```
.
├── Controllers/
│   ├── UsersController.cs                # CRUD endpoints
│   └── DiagnosticsController.cs          # Dev-only forced-exception endpoint
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs        # try/catch → JSON 500
│   ├── TokenAuthenticationMiddleware.cs  # Bearer-token gate
│   └── RequestLoggingMiddleware.cs       # method/path/status/elapsed logs
├── Models/
│   ├── User.cs                           # User entity + request DTOs
│   └── PagedResult.cs                    # Pagination envelope
├── Services/
│   ├── IUserService.cs                   # Storage contract
│   └── InMemoryUserService.cs            # Thread-safe in-memory store
├── Validation/NotWhitespaceAttribute.cs  # Custom validator
├── Properties/launchSettings.json        # Local run profiles
├── Program.cs                            # Bootstrap, DI, Swagger, middleware pipeline
├── appsettings.json                      # Base config (placeholder token)
├── appsettings.Development.json          # Dev overrides (real dev token)
├── UserManagementAPI.http                # Sample + middleware test requests
├── UserManagementAPI.csproj
├── COPILOT_NOTES.md                      # Activity 1 deliverable
├── DEBUGGING_NOTES.md                    # Activity 2 deliverable
└── MIDDLEWARE_NOTES.md                   # Activity 3 deliverable
```

## Activity deliverables

- [`COPILOT_NOTES.md`](./COPILOT_NOTES.md) — how Copilot helped scaffold the API (activity 1).
- [`DEBUGGING_NOTES.md`](./DEBUGGING_NOTES.md) — bugs caught and fixes applied (activity 2).
- [`MIDDLEWARE_NOTES.md`](./MIDDLEWARE_NOTES.md) — middleware pipeline (logging, error, auth) for activity 3.
