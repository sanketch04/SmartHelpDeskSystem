
namespace SmartHelpdesk.BLL.Interfaces;

public interface IProfileImageService
{
    #region Methods

    Task<string> SaveAsync(
        Stream imageStream,
        string extension,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string? imageUrl,
        CancellationToken cancellationToken = default);

    #endregion
}
