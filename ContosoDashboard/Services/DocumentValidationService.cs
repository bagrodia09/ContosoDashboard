using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoDashboard.Services;

public interface IDocumentValidationService
{
    DocumentValidationResult Validate(DocumentUploadRequest request);
    IReadOnlyList<DocumentUploadResult> ValidateBatch(IEnumerable<DocumentUploadRequest> requests);
}

public class DocumentValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DocumentValidationService : IDocumentValidationService
{
    private const long MaxFileSizeBytes = 26_214_400;
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    };

    private static readonly Dictionary<string, string[]> AllowedMimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
        [".txt"] = ["text/plain"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"]
    };

    public DocumentValidationResult Validate(DocumentUploadRequest request)
    {
        var result = new DocumentValidationResult { IsValid = true };
        if (request is null)
        {
            result.IsValid = false;
            result.Errors.Add("No file metadata was provided.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            result.Errors.Add("Title is required.");
        }
        else if (request.Title.Trim().Length > 200)
        {
            result.Errors.Add("Title cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            result.Errors.Add("Category is required.");
        }
        else if (!AllowedCategories.Contains(request.Category.Trim()))
        {
            result.Errors.Add("Category must be one of the supported values.");
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 2000)
        {
            result.Errors.Add("Description cannot exceed 2000 characters.");
        }

        if (!string.IsNullOrWhiteSpace(request.Tags) && request.Tags.Length > 1000)
        {
            result.Errors.Add("Tags cannot exceed 1000 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType) || request.ContentType.Length > 255)
        {
            result.Errors.Add("Content type is required and must be at most 255 characters.");
        }

        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxFileSizeBytes)
        {
            result.Errors.Add(request.FileSizeBytes > MaxFileSizeBytes
                ? "File exceeds the 25 MiB (26,214,400 byte) limit."
                : "File size must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            result.Errors.Add("A file name is required.");
        }
        else
        {
            var extension = Path.GetExtension(request.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                result.Errors.Add("The file extension is missing or unsupported.");
            }
            else if (!AllowedMimeMap.ContainsKey(extension))
            {
                result.Errors.Add("This file type is not supported. Use PDF, DOCX, XLSX, PPTX, TXT, JPG, JPEG, or PNG.");
            }
            else
            {
                var normalizedContentType = NormalizeMimeType(request.ContentType);
                var supportedTypes = AllowedMimeMap[extension];
                if (!supportedTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"Content type {normalizedContentType} does not match the expected type for {extension}.");
                }
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public IReadOnlyList<DocumentUploadResult> ValidateBatch(IEnumerable<DocumentUploadRequest> requests)
    {
        var items = requests?.Where(r => r is not null).ToList() ?? new List<DocumentUploadRequest>();
        var results = new List<DocumentUploadResult>();
        foreach (var item in items)
        {
            var validation = Validate(item);
            var result = new DocumentUploadResult
            {
                DisplayFileName = item.FileName,
                Status = validation.IsValid ? "ValidatedNotPersisted" : "Rejected",
                ValidationErrors = validation.Errors,
                Persisted = false
            };

            if (!validation.IsValid)
            {
                result.Status = "Rejected";
            }

            results.Add(result);
        }

        return results;
    }

    private static string NormalizeMimeType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        return contentType.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0].Trim();
    }
}
