using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public interface IUserService
{
    IEnumerable<User> GetAll();
    User? GetById(int id);
    User Create(CreateUserRequest request);
    User? Update(int id, UpdateUserRequest request);
    bool Delete(int id);
    bool EmailExists(string email, int? excludeId = null);
}
