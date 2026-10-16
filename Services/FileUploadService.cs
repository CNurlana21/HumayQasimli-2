using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace HumayQasimli.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly Cloudinary _cloudinary;

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

        public FileUploadService(IConfiguration configuration)
        {
            _configuration = configuration;

            var url = configuration["Cloudinary:Url"]
                      ?? throw new InvalidOperationException("Cloudinary:Url teyin olunmayib.");
            _cloudinary = new Cloudinary(url);
            _cloudinary.Api.Secure = true;
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

            var folder = $"humayqasimli/{subFolder}";
            var publicId = Guid.NewGuid().ToString("N");

            try
            {
                await using var stream = file.OpenReadStream();
                UploadResult result;

                if (VideoExtensions.Contains(extension))
                {
                    result = await _cloudinary.UploadAsync(new VideoUploadParams
                    {
                        File = new FileDescription(publicId + extension, stream),
                        Folder = folder,
                        PublicId = publicId
                    });
                }
                else if (DocumentExtensions.Contains(extension))
                {
                    result = await _cloudinary.UploadAsync(new RawUploadParams
                    {
                        File = new FileDescription(publicId + extension, stream),
                        Folder = folder,
                        PublicId = publicId + extension
                    });
                }
                else
                {
                    result = await _cloudinary.UploadAsync(new ImageUploadParams
                    {
                        File = new FileDescription(publicId + extension, stream),
                        Folder = folder,
                        PublicId = publicId
                    });
                }

                if (result.Error != null)
                {
                    return (false, null, "Yukleme xetasi: " + result.Error.Message);
                }

                return (true, result.SecureUrl.ToString(), null);
            }
            catch (Exception ex)
            {
                return (false, null, "Yukleme xetasi: " + ex.Message);
            }
        }

        public void DeleteFile(string? relativePath)
        {
            // Kohne yerli fayllar (/uploads/...) ve bos deyerler ucun hec ne etmirik
            if (string.IsNullOrWhiteSpace(relativePath) ||
                !relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                // https://res.cloudinary.com/{cloud}/{image|video|raw}/upload/v123/folder/name.ext
                var segments = new Uri(relativePath).AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex < 1) return;

                var resourceTypeName = segments[uploadIndex - 1];
                var rest = segments.Skip(uploadIndex + 1).ToList();

                if (rest.Count > 0 && System.Text.RegularExpressions.Regex.IsMatch(rest[0], @"^v\d+$"))
                {
                    rest.RemoveAt(0);
                }

                var publicId = Uri.UnescapeDataString(string.Join("/", rest));

                var resourceType = resourceTypeName switch
                {
                    "video" => ResourceType.Video,
                    "raw" => ResourceType.Raw,
                    _ => ResourceType.Image
                };

                // Sekil ve video ucun public id uzantisiz olur, raw (pdf) ucun uzantili
                if (resourceType != ResourceType.Raw)
                {
                    var dot = publicId.LastIndexOf('.');
                    if (dot > 0) publicId = publicId.Substring(0, dot);
                }

                _cloudinary.Destroy(new DeletionParams(publicId) { ResourceType = resourceType });
            }
            catch
            {
                // Silinmese de sayt islemeye davam etsin
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