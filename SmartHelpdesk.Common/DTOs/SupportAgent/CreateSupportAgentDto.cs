using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.SupportAgents
{
    public class CreateSupportAgentDto
    {
        #region Properties

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        public IFormFile? ProfileImage { get; set; }

        #endregion
    }
}