
namespace Application.DTOs.User;

public class UserDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public string Email { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();
}
