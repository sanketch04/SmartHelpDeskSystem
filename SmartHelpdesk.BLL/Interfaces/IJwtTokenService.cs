namespace SmartHelpdesk.DAL.Entities;
public interface IJwtTokenService
{
    #region Methods

    Task<string> GenerateAccessTokenAsync(ApplicationUser user);

    #endregion
}
