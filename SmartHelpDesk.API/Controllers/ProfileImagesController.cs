
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.Common.DTOs.ProfileImages;
using SmartHelpdesk.DAL.Entities;

namespace SmartHelpdesk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class ProfileImagesController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProfileImageService _profileImageService;
        private readonly ILogger<ProfileImagesController> _logger;

        #region Ctor

        public ProfileImagesController(UserManager<ApplicationUser> userManager,
            IProfileImageService profileImageService,
            ILogger<ProfileImagesController> logger)
        {
            _userManager = userManager;
            _profileImageService = profileImageService;
            _logger = logger;
        }

        #endregion

        #region Upload Or Replace Profile Image

        [HttpPut("me")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
        public async Task<IActionResult> UploadMyImage(
            [FromForm] UploadProfileImageDto request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Your account is unavailable."
                });
            }

            var image = request.Image;

            if (image == null || image.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select an image."
                });
            }

            if (image.Length > MaxFileSize)
            {
                return BadRequest(new
                {
                    message = "The maximum image size is 5 MB."
                });
            }

            var extension = await ValidateImageAsync(image);

            if (extension == null)
            {
                return BadRequest(new
                {
                    message = "Only valid JPEG, PNG, and WebP images are allowed."
                });
            }

            string? newImageUrl = null;
            var previousImageUrl = user.ProfileImageUrl;

            try
            {
                await using (var stream = image.OpenReadStream())
                {
                    newImageUrl = await _profileImageService.SaveAsync(
                        stream,
                        extension,
                        cancellationToken);
                }

                user.ProfileImageUrl = newImageUrl;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    await _profileImageService.DeleteAsync(
                        newImageUrl,
                        cancellationToken);

                    return BadRequest(new
                    {
                        message = "Could not update the profile image.",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                // Remove the previous image only after the DB update succeeds.
                await _profileImageService.DeleteAsync(previousImageUrl,cancellationToken);

                return Ok(new
                {
                    message = "Profile image updated successfully.",
                    profileImageUrl = newImageUrl
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error uploading profile image for user {UserId}",
                    user.Id);

                if (!string.IsNullOrWhiteSpace(newImageUrl))
                {
                    try
                    {
                        await _profileImageService.DeleteAsync(
                            newImageUrl,
                            CancellationToken.None);
                    }
                    catch (Exception cleanupException)
                    {
                        _logger.LogWarning(
                            cleanupException,
                            "Could not clean up an uploaded profile image.");
                    }
                }

                return StatusCode(500, new
                {
                    message = "An error occurred while uploading the image."
                });
            }
        }

        #endregion

        #region Delete My Profile Image

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyImage(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Your account is unavailable."
                });
            }

            var previousImageUrl = user.ProfileImageUrl;

            if (string.IsNullOrWhiteSpace(previousImageUrl))
            {
                return Ok(new
                {
                    message = "No profile image is set.",
                    profileImageUrl = (string?)null
                });
            }

            user.ProfileImageUrl = null;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Could not remove the profile image.",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            try
            {
                await _profileImageService.DeleteAsync(
                    previousImageUrl,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // The DB is already updated. Log cleanup failure for maintenance.
                _logger.LogWarning(
                    ex,
                    "Could not delete old profile image for user {UserId}",
                    user.Id);
            }

            return Ok(new
            {
                message = "Profile image removed successfully.",
                profileImageUrl = (string?)null
            });
        }

        #endregion

        #region Image Validation

        private static async Task<string?> ValidateImageAsync(
            IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (extension is not ".jpg" and not ".jpeg"
                and not ".png" and not ".webp")
            {
                return null;
            }

            var allowedContentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => string.Empty
            };

            if (!string.Equals(
                    image.ContentType,
                    allowedContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var header = new byte[12];

            await using var stream = image.OpenReadStream();

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
    }
}
