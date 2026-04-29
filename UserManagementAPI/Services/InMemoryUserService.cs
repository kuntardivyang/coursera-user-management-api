using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class InMemoryUserService : IUserService
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId;

    public InMemoryUserService()
    {
        Seed(new CreateUserRequest
        {
            FirstName = "Asha",
            LastName = "Patel",
            Email = "asha.patel@techhive.example",
            Department = "HR",
            Role = "Manager"
        });
        Seed(new CreateUserRequest
        {
            FirstName = "Marcus",
            LastName = "Chen",
            Email = "marcus.chen@techhive.example",
            Department = "IT",
            Role = "Engineer"
        });
    }

    public IEnumerable<User> GetAll() => _users.Values.OrderBy(u => u.Id);

    public User? GetById(int id) => _users.TryGetValue(id, out var user) ? user : null;

    public User Create(CreateUserRequest request)
    {
        var id = Interlocked.Increment(ref _nextId);
        var user = new User
        {
            Id = id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Department = request.Department.Trim(),
            Role = request.Role.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _users[id] = user;
        return user;
    }

    public User? Update(int id, UpdateUserRequest request)
    {
        if (!_users.TryGetValue(id, out var existing))
        {
            return null;
        }

        var updated = new User
        {
            Id = existing.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Department = request.Department.Trim(),
            Role = request.Role.Trim(),
            CreatedAt = existing.CreatedAt
        };
        _users[id] = updated;
        return updated;
    }

    public bool Delete(int id) => _users.TryRemove(id, out _);

    public bool EmailExists(string email, int? excludeId = null)
    {
        var normalized = email.Trim();
        return _users.Values.Any(u =>
            string.Equals(u.Email, normalized, StringComparison.OrdinalIgnoreCase)
            && (excludeId is null || u.Id != excludeId));
    }

    private void Seed(CreateUserRequest request) => Create(request);
}
