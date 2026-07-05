using System.ComponentModel.DataAnnotations;

namespace Tiketin.Web.Contracts;

public record UserListItem(
    Guid Id,
    string FullName,
    string Email,
    string Department,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(120)] string FullName,
    [Required, MaxLength(80)] string Department,
    [Required] string Role,
    [Required, MinLength(8)] string Password);

public record UpdateUserRequest(
    [Required, MaxLength(120)] string FullName,
    [Required, MaxLength(80)] string Department,
    [Required] string Role,
    bool IsActive);
