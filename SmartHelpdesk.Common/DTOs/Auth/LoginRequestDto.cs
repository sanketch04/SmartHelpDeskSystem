
namespace SmartHelpdesk.Common.DTOs.Auth;

public class LoginRequestDto
{
    #region Properties

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    #endregion
}
