using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class InMemoryUserService : IUserService
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private readonly object _writeLock = new();
    private int _nextId;

    public InMemoryUserService()
    {
        SeedUnsafe(new CreateUserRequest
        {
            FirstName = "Asha",
            LastName = "Patel",
            Email = "asha.patel@techhive.example",
            Department = "HR",
            Role = "Manager"
        });
        SeedUnsafe(new CreateUserRequest
        {
            FirstName = "Marcus",
            LastName = "Chen",
            Email = "marcus.chen@techhive.example",
            Department = "IT",
            Role = "Engineer"
        });
    }

    public PagedResult<User> GetPaged(int page, int pageSize)
    {
        // Snapshot once so total + page slice agree even under concurrent writes.
        var snapshot = _users.Values
            .OrderBy(u => u.Id)
            .ToList();

        var items = snapshot
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<User>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = snapshot.Count,
            Items = items
        };
    }

    public User? GetById(int id) =>
        _users.TryGetValue(id, out var user) ? user : null;

    public User? TryCreate(CreateUserRequest request)
    {
        var normalized = Normalize(request.Email);

        lock (_writeLock)
        {
            if (EmailExistsLocked(normalized, excludeId: null))
            {
                return null;
            }

            var id = Interlocked.Increment(ref _nextId);
            var user = new User
            {
                Id = id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalized,
                Department = request.Department.Trim(),
                Role = request.Role.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _users[id] = user;
            return user;
        }
    }

    public (User? Updated, bool EmailConflict) TryUpdate(int id, UpdateUserRequest request)
    {
        var normalized = Normalize(request.Email);

        lock (_writeLock)
        {
            if (!_users.TryGetValue(id, out var existing))
            {
                return (null, false);
            }

            if (EmailExistsLocked(normalized, excludeId: id))
            {
                return (null, true);
            }

            var updated = new User
            {
                Id = existing.Id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalized,
                Department = request.Department.Trim(),
                Role = request.Role.Trim(),
                CreatedAt = existing.CreatedAt
            };
            _users[id] = updated;
            return (updated, false);
        }
    }

    public bool Delete(int id) => _users.TryRemove(id, out _);

    private bool EmailExistsLocked(string normalizedEmail, int? excludeId)
    {
        foreach (var u in _users.Values)
        {
            if (excludeId is int ex && u.Id == ex) continue;
            if (string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string Normalize(string email) => email.Trim();

    private void SeedUnsafe(CreateUserRequest request)
    {
        // Constructor-only; not subject to concurrency.
        TryCreate(request);
    }
}
