using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.Common.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartHelpdesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    #region Fields

    private readonly IAuthService _authService;

    #endregion

    #region Ctor

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    #endregion

    #region Admin Test

    [Authorize(Policy = "AdministratorOnly")]
    
    [HttpGet("admin-test")]
    public IActionResult AdminTest()
    {
        return Ok(new
        {
            message = "Administrator access granted."
        });
    }

    #endregion

    #region Register Employee

    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromForm] RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message =
                    "Full name, email, and password are required."
            });
        }

        // RegisterRequestDto must contain:
        // public IFormFile? ProfileImage { get; set; }
        //
        // AuthService must save the optional image through
        // IProfileImageService and persist ProfileImageUrl.

        var result = await _authService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(
                    error => error.Description)
            });
        }

        return Ok(new
        {
            message = "Registration successful."
        });
    }

    #endregion

    #region Login

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Email and password are required."
            });
        }

        var response = await _authService.LoginAsync(request);

        if (response is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        return Ok(response);
    }

    #endregion

    #region Refresh Token

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDto request)
    {
        var response =
            await _authService.RefreshTokenAsync(request);

        if (response is null)
        {
            return Unauthorized(new
            {
                message =
                    "Refresh token is invalid, expired, or revoked."
            });
        }

        return Ok(response);
    }

    #endregion

    #region Logout

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        return NoContent();
    }

    #endregion
}