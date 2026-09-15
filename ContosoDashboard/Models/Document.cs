using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(255)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Tags { get; set; }

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [Required]
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    [Required]
    public long FileSizeBytes { get; set; }

    [Required]
    [MaxLength(255)]
    public string ContentType { get; set; } = string.Empty;
}

public class DocumentUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public long FileSizeBytes { get; set; }
    public Stream? FileStream { get; set; }
}

public class DocumentUploadResult
{
    public string DisplayFileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Rejected";
    public List<string> ValidationErrors { get; set; } = new();
    public bool Persisted { get; set; }
}

public class DocumentContentResult
{
    public Document Document { get; set; } = default!;
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
}
