namespace proekt_za_6ca.Services
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile imageFile, HttpRequest request);
        Task<bool> DeleteImageAsync(string imageUrl);
        bool IsValidImage(IFormFile file);
    }
}