using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IUserService _users;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService users, ILogger<UsersController> logger)
    {
        _users = users;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<User>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<PagedResult<User>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            return ValidationProblem("page must be >= 1.");
        }
        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            return ValidationProblem($"pageSize must be between 1 and {MaxPageSize}.");
        }

        return Ok(_users.GetPaged(page, pageSize));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<User> GetById(int id)
    {
        if (id <= 0)
        {
            return ValidationProblem("id must be a positive integer.");
        }

        var user = _users.GetById(id);
        if (user is null)
        {
            _logger.LogInformation("Lookup miss for user {UserId}", id);
            return NotFoundProblem($"User {id} not found.");
        }
        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<User> Create([FromBody] CreateUserRequest request)
    {
        var created = _users.TryCreate(request);
        if (created is null)
        {
            return ConflictProblem($"Email '{request.Email.Trim()}' is already in use.");
        }

        _logger.LogInformation("Created user {UserId} ({Email})", created.Id, created.Email);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<User> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (id <= 0)
        {
            return ValidationProblem("id must be a positive integer.");
        }

        var (updated, emailConflict) = _users.TryUpdate(id, request);
        if (updated is null && emailConflict)
        {
            return ConflictProblem($"Email '{request.Email.Trim()}' is already in use by another user.");
        }
        if (updated is null)
        {
            return NotFoundProblem($"User {id} not found.");
        }

        _logger.LogInformation("Updated user {UserId}", id);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        if (id <= 0)
        {
            return ValidationProblem("id must be a positive integer.");
        }

        if (!_users.Delete(id))
        {
            return NotFoundProblem($"User {id} not found.");
        }
        _logger.LogInformation("Deleted user {UserId}", id);
        return NoContent();
    }

    private ObjectResult ValidationProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Invalid request.");

    private ObjectResult NotFoundProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status404NotFound, title: "Resource not found.");

    private ObjectResult ConflictProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict.");
}
