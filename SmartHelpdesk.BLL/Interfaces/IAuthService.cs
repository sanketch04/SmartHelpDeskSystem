
using Microsoft.AspNetCore.Identity;
using SmartHelpdesk.Common.DTOs.Auth;

namespace SmartHelpdesk.BLL.Interfaces;

public interface IAuthService
{
    #region Methods

    Task<IdentityResult> RegisterAsync(
        RegisterRequestDto request);

    Task<AuthResponseDto?> LoginAsync(
        LoginRequestDto request);

    Task<AuthResponseDto?> RefreshTokenAsync(
        RefreshTokenRequestDto request);

    Task RevokeRefreshTokenAsync(
        RefreshTokenRequestDto request);

    #endregion
}
