using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace ChatApp.WebAPI.Services
{
    public interface IMediaService
    {
        /// <summary>
        /// Lưu file vào uploads/{category}/{yyyy-MM-dd}, sinh thumbnail nếu là ảnh,
        /// tạo record MediaFile, trả về MediaFileId.
        /// </summary>
        Task<MediaFile> SaveMediaFileAsync(IFormFile file, string userId);
        Task<MediaFile> GetMediaFileAsync(int mediaFileId, string userId);
        Task<List<MediaFile>> GetMediaFilesAsync(List<int> mediaFileIds, string userId);
    }
    public class MediaService : IMediaService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IApplicationDbContext _context;

        // max 50MB
        private const long _maxSize = 50L * 1024 * 1024;
        private static readonly string[] _allowedPrefixes = {
                                                            "image/", "video/", "audio/", "application/pdf", "text/",
                                                            "application/msword",
                                                            "application/vnd.openxmlformats-officedocument"
                                                        };

        public MediaService(IWebHostEnvironment env, IApplicationDbContext context)
        {
            _env = env;
            _context = context;
        }

        public async Task<MediaFile> SaveMediaFileAsync(IFormFile file, string userId)
        {
            // 1. Validate
            if (file.Length == 0 || file.Length > _maxSize)
                throw new InvalidOperationException("File size invalid");
            if (!_allowedPrefixes.Any(p => file.ContentType.StartsWith(p)))
                throw new InvalidOperationException("File type not allowed");

            // 2. Xác định category dựa trên MIME type
            var category = file.ContentType switch
            {
                var ct when ct.StartsWith("image/") => "images",
                var ct when ct.StartsWith("video/") => "videos",
                var ct when ct.StartsWith("audio/") => "audios",
                _ => "others"
            };

            // 3. Xác định folder root và ngày hiện tại
            string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");

            if(!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            // 4. Tạo thư mục lưu file và (nếu ảnh) thư mục thumbnails
            string saveFolder = Path.Combine(uploadsRoot, category, dateFolder);
            string? thumbnailFolder = file.ContentType.StartsWith("image/")
                ? Path.Combine(uploadsRoot, category, "thumbnails", dateFolder)
                : null;

            // Tạo directory chỉ khi cần, CreateDirectory sẽ bỏ qua nếu đã tồn tại
            Directory.CreateDirectory(saveFolder);
            if (thumbnailFolder is not null)
                Directory.CreateDirectory(thumbnailFolder);

            // 5. Tên file và lưu file
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string fullPath = Path.Combine(saveFolder, fileName);
            await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs);
            }

            // 6. Tạo bản ghi MediaFile
            var media = new MediaFile
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FilePath = $"/uploads/{category}/{dateFolder}/{fileName}",
                FileSize = file.Length,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow
            };

            // 7. Nếu là ảnh, sinh thumbnail
            if (thumbnailFolder is not null)
            {
                string thumbPath = Path.Combine(thumbnailFolder, fileName);
                await GenerateThumbnail(fullPath, thumbPath);
                media.ThumbnailPath = $"/uploads/{category}/thumbnails/{dateFolder}/{fileName}";
            }

            // 8. Lưu DB
            _context.MediaFiles.Add(media);
            await _context.SaveChangesAsync();

            return media;
        }


        private async Task GenerateThumbnail(string sourcePath, string destPath)
        {
            // Kích thước thumbnail bạn muốn (ví dụ 200x200)
            const int thumbnailWidth = 200;
            const int thumbnailHeight = 200;

            using (Image image = await Image.LoadAsync(sourcePath))
            {
                // Tính tỉ lệ để giữ nguyên tỉ lệ gốc, đồng thời cover vùng 200x200
                var options = new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(thumbnailWidth, thumbnailHeight)
                };

                image.Mutate(x => x.Resize(options));

                // Tự động chọn định dạng đầu ra dựa vào đuôi file
                IImageEncoder encoder = GetEncoderByExtension(Path.GetExtension(destPath));

                // Tạo thư mục đích nếu chưa tồn tại
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                await image.SaveAsync(destPath, encoder);
            }
        }

        private IImageEncoder GetEncoderByExtension(string ext)
        {
            ext = ext.ToLowerInvariant();
            return ext switch
            {
                ".png" => new SixLabors.ImageSharp.Formats.Png.PngEncoder(),
                ".jpg" or ".jpeg" => new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 75 },
                ".gif" => new SixLabors.ImageSharp.Formats.Gif.GifEncoder(),
                _ => new SixLabors.ImageSharp.Formats.Png.PngEncoder()
            };
        }

        public async Task<MediaFile> GetMediaFileAsync(int mediaFileId, string userId)
        {
            return await _context.MediaFiles
                .Where(m => m.FileId == mediaFileId && !m.IsDeleted)
                .Select(m => new MediaFile
                {
                    FileId = m.FileId,
                    FileName = m.FileName,
                    OriginalFileName = m.OriginalFileName,
                    ContentType = m.ContentType,
                    FilePath = m.FilePath,
                    ThumbnailPath = m.ThumbnailPath,
                    FileSize = m.FileSize,
                    UploadedBy = userId,
                    UploadedAt = m.UploadedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<MediaFile>> GetMediaFilesAsync(List<int> mediaFileIds, string userId)
        {
            return await _context.MediaFiles
                .Where(m => mediaFileIds.Contains(m.FileId) && !m.IsDeleted)
                .Select(m => new MediaFile
                {
                    FileId = m.FileId,
                    FileName = m.FileName,
                    OriginalFileName = m.OriginalFileName,
                    ContentType = m.ContentType,
                    FilePath = m.FilePath,
                    ThumbnailPath = m.ThumbnailPath,
                    FileSize = m.FileSize,
                    UploadedBy = userId,
                    UploadedAt = m.UploadedAt
                })
                .ToListAsync();
        }
    }
}
