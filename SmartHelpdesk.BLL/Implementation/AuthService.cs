
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.Common.DTOs.Auth;
using SmartHelpdesk.DAL.Context;
using SmartHelpdesk.DAL.Entities;

namespace SmartHelpdesk.BLL.Services;

public class AuthService : IAuthService
{
    #region Constants

    private const long MaxProfileImageSize = 5 * 1024 * 1024;

    #endregion

    #region Fields

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProfileImageService _profileImageService;
    private readonly AppDbContext _context;

    #endregion

    #region Ctor

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService jwtTokenService,
        IProfileImageService profileImageService,
        AppDbContext context)
    {
        _userManager = userManager
            ?? throw new ArgumentNullException(nameof(userManager));

        _roleManager = roleManager
            ?? throw new ArgumentNullException(nameof(roleManager));

        _jwtTokenService = jwtTokenService
            ?? throw new ArgumentNullException(nameof(jwtTokenService));

        _profileImageService = profileImageService
            ?? throw new ArgumentNullException(nameof(profileImageService));

        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    #endregion

    #region Public Methods

    #region Register Employee

    public async Task<IdentityResult> RegisterAsync(
        RegisterRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim();
        var fullName = request.FullName?.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidRegistration",
                Description =
                    "Full name, email, and password are required."
            });
        }

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description =
                    "An account with this email already exists."
            });
        }

        // Validate the optional image before saving anything.
        string? imageExtension = null;

        if (request.ProfileImage is not null)
        {
            imageExtension = await ValidateProfileImageAsync(
                request.ProfileImage);

            if (imageExtension is null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidProfileImage",
                    Description =
                        "Please upload a valid JPEG, PNG, or WebP image " +
                        "not exceeding 5 MB."
                });
            }
        }

        const string defaultRole = "Employee";

        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            var roleResult = await _roleManager.CreateAsync(
                new IdentityRole(defaultRole));

            if (!roleResult.Succeeded)
            {
                return roleResult;
            }
        }

        string? profileImageUrl = null;
        ApplicationUser? createdUser = null;

        try
        {
            // A profile image is optional during registration.
            if (request.ProfileImage is not null &&
                imageExtension is not null)
            {
                await using var imageStream =
                    request.ProfileImage.OpenReadStream();

                profileImageUrl =
                    await _profileImageService.SaveAsync(
                        imageStream,
                        imageExtension);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                ProfileImageUrl = profileImageUrl
            };

            var createResult = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!createResult.Succeeded)
            {
                await TryDeleteProfileImageAsync(profileImageUrl);
                return createResult;
            }

            createdUser = user;

            var assignmentResult = await _userManager.AddToRoleAsync(
                user,
                defaultRole);

            if (!assignmentResult.Succeeded)
            {
                var deleteUserResult =
                    await _userManager.DeleteAsync(user);

                if (deleteUserResult.Succeeded)
                {
                    await TryDeleteProfileImageAsync(profileImageUrl);
                }

                return assignmentResult;
            }

            return IdentityResult.Success;
        }
        catch
        {
            // If registration fails after user creation, attempt to
            // remove the incomplete account before deleting its image.
            if (createdUser is not null)
            {
                var deleteUserResult =
                    await _userManager.DeleteAsync(createdUser);

                if (deleteUserResult.Succeeded)
                {
                    await TryDeleteProfileImageAsync(profileImageUrl);
                }
            }
            else
            {
                await TryDeleteProfileImageAsync(profileImageUrl);
            }

            throw;
        }
    }

    #endregion

    #region Login

    public async Task<AuthResponseDto?> LoginAsync(
        LoginRequestDto request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user is null ||
            !user.IsActive ||
            !await _userManager.CheckPasswordAsync(
                user,
                request.Password))
        {
            return null;
        }

        return await CreateAuthResponseAsync(user);
    }

    #endregion

    #region Refresh Token

    public async Task<AuthResponseDto?> RefreshTokenAsync(
        RefreshTokenRequestDto request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var tokenHash = HashToken(request.RefreshToken);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken is null)
        {
            return null;
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            // Detect reuse of a revoked token and revoke remaining
            // active refresh tokens for this user.
            var activeTokens = await _context.RefreshTokens
                .Where(t =>
                    t.UserId == storedToken.UserId &&
                    t.RevokedAtUtc == null)
                .ToListAsync();

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return null;
        }

        if (storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(
            storedToken.UserId);

        if (user is null || !user.IsActive)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return null;
        }

        // Revoke the existing refresh token and issue a replacement.
        storedToken.RevokedAtUtc = DateTime.UtcNow;

        var response = await CreateAuthResponseAsync(user);

        storedToken.ReplacedByTokenHash =
            HashToken(response.RefreshToken);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return response;
    }

    #endregion

    #region Revoke Refresh Token

    public async Task RevokeRefreshTokenAsync(
        RefreshTokenRequestDto request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = HashToken(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken is null ||
            storedToken.RevokedAtUtc is not null)
        {
            return;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #endregion

    #region Private Methods

    #region Create Authentication Response

    private async Task<AuthResponseDto> CreateAuthResponseAsync(
        ApplicationUser user)
    {
        var now = DateTime.UtcNow;

        var accessToken = await _jwtTokenService
            .GenerateAccessTokenAsync(user);

        var refreshToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(7)
        };

        await _context.RefreshTokens.AddAsync(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            ProfileImageUrl = user.ProfileImageUrl,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = now.AddMinutes(15)
        };
    }

    #endregion

    #region Validate Profile Image

    private static async Task<string?> ValidateProfileImageAsync(
        IFormFile image)
    {
        if (image.Length <= 0 ||
            image.Length > MaxProfileImageSize)
        {
            return null;
        }

        var extension = Path.GetExtension(image.FileName)
            .ToLowerInvariant();

        var expectedContentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (expectedContentType is null ||
            !string.Equals(
                image.ContentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        await using var stream = image.OpenReadStream();

        var header = new byte[12];

        var bytesRead = await stream.ReadAsync(
            header.AsMemory(0, header.Length));

        if (extension is ".jpg" or ".jpeg")
        {
            return bytesRead >= 3 &&
                   header[0] == 0xFF &&
                   header[1] == 0xD8 &&
                   header[2] == 0xFF
                ? extension
                : null;
        }

        if (extension == ".png")
        {
            byte[] signature =
            {
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A
            };

            return bytesRead >= 8 &&
                   header.Take(8).SequenceEqual(signature)
                ? extension
                : null;
        }

        if (extension == ".webp")
        {
            return bytesRead >= 12 &&
                   header[0] == (byte)'R' &&
                   header[1] == (byte)'I' &&
                   header[2] == (byte)'F' &&
                   header[3] == (byte)'F' &&
                   header[8] == (byte)'W' &&
                   header[9] == (byte)'E' &&
                   header[10] == (byte)'B' &&
                   header[11] == (byte)'P'
                ? extension
                : null;
        }

        return null;
    }

    #endregion

    #region Delete Uploaded Image Safely

    private async Task TryDeleteProfileImageAsync(
        string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        try
        {
            await _profileImageService.DeleteAsync(imageUrl);
        }
        catch
        {
            // Preserve the original registration result or exception.
            // Add structured logging here in production.
        }
    }

    #endregion

    #region Hash Token

    private static string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    #endregion

    #endregion
}
