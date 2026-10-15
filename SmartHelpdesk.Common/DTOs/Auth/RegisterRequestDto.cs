using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.Auth;

public class RegisterRequestDto
{
    #region Properties

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public IFormFile? ProfileImage { get; set; }

    #endregion
}