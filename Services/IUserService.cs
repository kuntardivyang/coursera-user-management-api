using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public interface IUserService
{
    PagedResult<User> GetPaged(int page, int pageSize);
    User? GetById(int id);

    /// <summary>
    /// Creates a user atomically with respect to email uniqueness.
    /// Returns null if the email is already in use.
    /// </summary>
    User? TryCreate(CreateUserRequest request);

    /// <summary>
    /// Updates a user atomically with respect to email uniqueness.
    /// Returns (null, false) if the user does not exist; (null, true) if email collides.
    /// </summary>
    (User? Updated, bool EmailConflict) TryUpdate(int id, UpdateUserRequest request);

    bool Delete(int id);
}
