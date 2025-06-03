using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class MediaFile
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public long FileSize { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Media-specific properties
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; } // For audio/video in seconds

        // Navigation properties
        public ApplicationUser UploadedByUser { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
