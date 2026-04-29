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

| Method | Route             | Purpose                          |
| ------ | ----------------- | -------------------------------- |
| GET    | `/api/users`      | List all users                   |
| GET    | `/api/users/{id}` | Get a single user by id          |
| POST   | `/api/users`      | Create a new user                |
| PUT    | `/api/users/{id}` | Update an existing user          |
| DELETE | `/api/users/{id}` | Delete a user by id              |

The API validates request bodies via data annotations (required fields, email
format, length limits) and returns:

- `400 Bad Request` for invalid input (handled by `[ApiController]`)
- `404 Not Found` when the user id does not exist
- `409 Conflict` when an email collides with another user
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
├── Controllers/UsersController.cs   # CRUD endpoints
├── Models/User.cs                   # User entity + request DTOs
├── Services/IUserService.cs         # Storage contract
├── Services/InMemoryUserService.cs  # Thread-safe in-memory store
├── Program.cs                       # Bootstrap, DI, Swagger
├── appsettings*.json                # Logging config
├── Properties/launchSettings.json   # Local run profiles
├── UserManagementAPI.http           # Sample requests
└── UserManagementAPI.csproj
```

## See also

- [`COPILOT_NOTES.md`](./COPILOT_NOTES.md) — how Copilot contributed to this
  scaffold (required deliverable for the activity).
