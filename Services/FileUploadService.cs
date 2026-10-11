using HumayQasimli.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace HumayQasimli.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] DocumentExtensions = { ".pdf" };
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".webm" };

        private static readonly Dictionary<string, byte[][]> MagicNumbers = new()
        {
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            [".webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            [".mp4"] = new[] { System.Text.Encoding.ASCII.GetBytes("ftyp") },
            [".mov"] = new[] { System.Text.Encoding.ASCII.GetBytes("ftyp") },
            [".webm"] = new[] { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } }
        };

        private static readonly Dictionary<string, int> MagicNumberOffset = new()
        {
            [".mp4"] = 4,
            [".mov"] = 4
        };

        public FileUploadService(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        private long MaxFileSizeBytes =>
            long.TryParse(_configuration["FileUploadSettings:MaxFileSizeBytes"], out var v) ? v : 5 * 1024 * 1024;

        private long MaxVideoFileSizeBytes =>
            long.TryParse(_configuration["FileUploadSettings:MaxVideoFileSizeBytes"], out var v) ? v : 150L * 1024 * 1024;

        public async Task<(bool Success, string? RelativePath, string? Error)> SaveFileAsync(IFormFile file, string subFolder, bool allowPdf = false, bool allowVideo = false)
        {
            if (file == null || file.Length == 0)
            {
                return (false, null, "Fayl secilmeyib.");
            }

            var maxSize = allowVideo ? MaxVideoFileSizeBytes : MaxFileSizeBytes;
            if (file.Length > maxSize)
            {
                return (false, null, $"Fayl olcusu maksimum {maxSize / 1024 / 1024}MB ola biler.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = allowVideo
                ? VideoExtensions
                : (allowPdf ? ImageExtensions.Concat(DocumentExtensions).ToArray() : ImageExtensions);

            if (!allowed.Contains(extension))
            {
                var allowedList = allowVideo ? "MP4, MOV, WEBM" : ("JPG, JPEG, PNG, WEBP" + (allowPdf ? ", PDF" : ""));
                return (false, null, $"Icaze verilmeyen fayl tipi. Yalniz {allowedList} qebul edilir.");
            }

            if (!await HasValidMagicNumberAsync(file, extension))
            {
                return (false, null, "Fayl mezmunu uzantisi ile uygun gelmir.");
            }

            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(uploadsRoot);

            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/{subFolder}/{safeFileName}";
            return (true, relativePath, null);
        }

        public void DeleteFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var trimmed = relativePath.TrimStart('/');
            var fullPath = Path.Combine(_env.WebRootPath, trimmed.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { }
            }
        }

        private static async Task<bool> HasValidMagicNumberAsync(IFormFile file, string extension)
        {
            if (!MagicNumbers.TryGetValue(extension, out var signatures))
            {
                return true;
            }

            var offset = MagicNumberOffset.TryGetValue(extension, out var o) ? o : 0;
            var longestSig = signatures.Max(s => s.Length);

            using var stream = file.OpenReadStream();
            var buffer = new byte[offset + longestSig];
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            stream.Position = 0;

            foreach (var sig in signatures)
            {
                if (read >= offset + sig.Length && buffer.Skip(offset).Take(sig.Length).SequenceEqual(sig))
                {
                    return true;
                }
            }

            return false;
        }
    }
}