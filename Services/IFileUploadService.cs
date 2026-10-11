using Microsoft.AspNetCore.Http;

namespace HumayQasimli.Services
{
    public interface IFileUploadService
    {
        Task<(bool Success, string? RelativePath, string? Error)> SaveFileAsync(IFormFile file, string subFolder, bool allowPdf = false, bool allowVideo = false);

        void DeleteFile(string? relativePath);
    }
}