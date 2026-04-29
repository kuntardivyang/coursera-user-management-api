# UserManagementAPI

CRUD Web API for TechHive Solutions' internal HR & IT user records.
Built with ASP.NET Core 8 (controllers + Swagger), in-memory storage.

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
cd UserManagementAPI
dotnet restore
dotnet run
```

Then open Swagger UI: <http://localhost:5080/swagger>

## Endpoints

| Method | Route                               | Purpose                            |
| ------ | ----------------------------------- | ---------------------------------- |
| GET    | `/api/users?page=1&pageSize=20`     | List users, paginated              |
| GET    | `/api/users/{id}`                   | Get a single user by id            |
| POST   | `/api/users`                        | Create a new user                  |
| PUT    | `/api/users/{id}`                   | Update an existing user            |
| DELETE | `/api/users/{id}`                   | Delete a user by id                |

`pageSize` is capped at 100. Defaults: `page=1`, `pageSize=20`. Response shape
for the list endpoint is `PagedResult<User>` (`page`, `pageSize`, `totalCount`,
`totalPages`, `items`).

Validation is enforced via data annotations (required, email format, length
limits, custom `NotWhitespace`). All error responses use RFC 7807
`ProblemDetails` / `ValidationProblemDetails`. Status codes:

- `400 Bad Request` — invalid body, bad query params, non-positive id
- `404 Not Found` — unknown user id
- `409 Conflict` — duplicate email (atomically enforced under concurrent writes)
- `500 Internal Server Error` — caught by `GlobalExceptionHandler`, response is ProblemDetails (no stack trace in Production)
- `201 Created` (with `Location` header) on successful POST
- `204 No Content` on successful DELETE

## Testing with Postman / `.http`

The repo includes `UserManagementAPI.http` with sample requests for VS Code
REST Client / Rider. For Postman, import the same routes manually or use the
Swagger UI to generate calls.

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
UserManagementAPI/
├── Controllers/UsersController.cs        # CRUD endpoints
├── Middleware/GlobalExceptionHandler.cs  # IExceptionHandler → ProblemDetails
├── Models/User.cs                        # User entity + request DTOs
├── Models/PagedResult.cs                 # Pagination envelope
├── Services/IUserService.cs              # Storage contract
├── Services/InMemoryUserService.cs       # Thread-safe in-memory store
├── Validation/NotWhitespaceAttribute.cs  # Custom validator
├── Program.cs                            # Bootstrap, DI, Swagger, error handler
├── appsettings*.json                     # Logging config
├── Properties/launchSettings.json        # Local run profiles
├── UserManagementAPI.http                # Sample + edge-case requests
└── UserManagementAPI.csproj
```

## See also

- [`COPILOT_NOTES.md`](./COPILOT_NOTES.md) — how Copilot helped scaffold the API (activity 1).
- [`DEBUGGING_NOTES.md`](./DEBUGGING_NOTES.md) — bugs caught and fixes applied (activity 2).
