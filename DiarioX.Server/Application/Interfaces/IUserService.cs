using DiarioX.Server.Application.DTOs.Users;

namespace DiarioX.Server.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetAllAsync();
    Task<UserResponse?> GetByIdAsync(int id);
    Task<UserCommandResult> CreateAsync(UserRequest request);
    Task<UserCommandResult> UpdateAsync(int id, UserRequest request);
    Task<UserCommandResult> DeleteAsync(int id);
}
