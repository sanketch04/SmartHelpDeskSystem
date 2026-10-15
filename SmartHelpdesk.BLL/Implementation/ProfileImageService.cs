using Microsoft.AspNetCore.Hosting;
using SmartHelpdesk.BLL.Interfaces;

namespace SmartHelpdesk.BLL.Services
{
    public class ProfileImageService : IProfileImageService
    {
        private readonly IWebHostEnvironment _environment;

        #region Ctor

        public ProfileImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        #endregion

        #region Save Image

        public async Task<string> SaveAsync(
            Stream imageStream,
            string extension,
            CancellationToken cancellationToken = default)
        {
            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var uploadFolder = Path.Combine(
                webRoot,
                "uploads",
                "profile-images");

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            await using var output = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

            await imageStream.CopyToAsync(
                output,
                cancellationToken);

            return $"/uploads/profile-images/{fileName}";
        }

        #endregion

        #region Delete Image

        public Task DeleteAsync(
            string? imageUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return Task.CompletedTask;
            }

            // Delete only files inside our dedicated profile-image folder.
            var fileName = Path.GetFileName(imageUrl);

            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName != imageUrl.Split('/').Last())
            {
                return Task.CompletedTask;
            }

            var webRoot = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var uploadFolder = Path.GetFullPath(
                Path.Combine(webRoot, "uploads", "profile-images"));

            var filePath = Path.GetFullPath(
                Path.Combine(uploadFolder, fileName));

            if (filePath.StartsWith(
                    uploadFolder + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
