
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.ProfileImages
{
    public class UploadProfileImageDto
    {
        #region Properties

        [Required]
        public IFormFile? Image { get; set; }

        #endregion
    }
}
