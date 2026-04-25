using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace proekt_za_6ca.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 2 * 1024 * 1024; // 2MB

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveImageAsync(IFormFile imageFile, HttpRequest request)
        {
            try
            {
                if (!IsValidImage(imageFile))
                {
                    throw new ArgumentException("Invalid image file provided.");
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                
                // Create upload path
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "restaurants");
                Directory.CreateDirectory(uploadsFolder);
                
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                // Generate full URL
                var scheme = request.Scheme;
                var host = request.Host.Value;
                var fullUrl = $"{scheme}://{host}/uploads/restaurants/{fileName}";

                _logger.LogInformation("Image saved successfully: {FileName}", fileName);
                return fullUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image: {FileName}", imageFile?.FileName);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl) || !imageUrl.Contains("/uploads/restaurants/"))
                {
                    return false; // Not a local image or invalid URL
                }

                // Extract filename from URL
                var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "restaurants", fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Image deleted successfully: {FileName}", fileName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImageUrl}", imageUrl);
                return false;
            }
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            // Check file size
            if (file.Length > MaxFileSize)
            {
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
            {
                return false;
            }

            return true;
        }
    }
}