
using Application.ApiResult;
using Application.DTOs.Auth;
using Application.DTOs.User;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(RegisterDto dto);

    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto);

    Task<Result<UserDto>> GetMeAsync(int userId);
}
