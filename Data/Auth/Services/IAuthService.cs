using TunnelSecurity.Auth.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace TunnelSecurity.Auth.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<LoginResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
        Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    }
}