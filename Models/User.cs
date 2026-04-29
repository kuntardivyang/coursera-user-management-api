using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Validation;

namespace UserManagementAPI.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [NotWhitespace]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50)]
    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateUserRequest
{
    [Required]
    [NotWhitespace]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50)]
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    [Required]
    [NotWhitespace]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [NotWhitespace]
    [StringLength(50)]
    public string Role { get; set; } = string.Empty;
}
