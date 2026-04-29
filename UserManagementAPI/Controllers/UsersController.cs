using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService users, ILogger<UsersController> logger)
    {
        _users = users;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<User>> GetAll() => Ok(_users.GetAll());

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> GetById(int id)
    {
        var user = _users.GetById(id);
        if (user is null)
        {
            return NotFound(new { message = $"User {id} not found." });
        }
        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<User> Create([FromBody] CreateUserRequest request)
    {
        if (_users.EmailExists(request.Email))
        {
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });
        }

        var created = _users.Create(request);
        _logger.LogInformation("Created user {UserId} ({Email})", created.Id, created.Email);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<User> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (_users.GetById(id) is null)
        {
            return NotFound(new { message = $"User {id} not found." });
        }

        if (_users.EmailExists(request.Email, excludeId: id))
        {
            return Conflict(new { message = $"Email '{request.Email}' is already in use by another user." });
        }

        var updated = _users.Update(id, request);
        _logger.LogInformation("Updated user {UserId}", id);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        if (!_users.Delete(id))
        {
            return NotFound(new { message = $"User {id} not found." });
        }
        _logger.LogInformation("Deleted user {UserId}", id);
        return NoContent();
    }
}
