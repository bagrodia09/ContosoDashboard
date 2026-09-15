using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDocumentValidationService _validationService;
    private readonly IDocumentAuthorizationService _documentAuthorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IDocumentValidationService validationService,
        IDocumentAuthorizationService documentAuthorizationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _validationService = validationService;
        _documentAuthorizationService = documentAuthorizationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Document>> GetOwnedDocumentsAsync()
    {
        var userId = GetCurrentUserId();
        return await _context.Documents
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();
    }

    public async Task<DocumentUploadResult[]> UploadBatchAsync(IEnumerable<DocumentUploadRequest> requests, CancellationToken cancellationToken = default)
    {
        var requestList = (requests ?? Enumerable.Empty<DocumentUploadRequest>()).ToList();
        if (requestList.Count == 0)
        {
            return new[]
            {
                new DocumentUploadResult
                {
                    DisplayFileName = string.Empty,
                    Status = "Rejected",
                    ValidationErrors = new List<string> { "No files were selected for upload." },
                    Persisted = false
                }
            };
        }

        var results = new List<DocumentUploadResult>();
        foreach (var request in requestList)
        {
            var validation = _validationService.Validate(request);
            var result = new DocumentUploadResult
            {
                DisplayFileName = request.FileName,
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

        if (results.Any(r => r.ValidationErrors.Count > 0))
        {
            foreach (var result in results)
            {
                result.Status = result.ValidationErrors.Count == 0 ? "ValidatedNotPersisted" : "Rejected";
                result.Persisted = false;
            }

            return results.ToArray();
        }

        var savedPaths = new List<string>();
        var currentUserId = GetCurrentUserId();

        try
        {
            foreach (var request in requestList)
            {
                if (request.FileStream is null)
                {
                    throw new InvalidOperationException($"No file stream was provided for {request.FileName}.");
                }

                var relativePath = await _fileStorageService.SaveAsync(request.FileStream, request.FileName, cancellationToken);
                savedPaths.Add(relativePath);

                var document = new Document
                {
                    Title = request.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                    Category = request.Category.Trim(),
                    Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim(),
                    OriginalFileName = request.FileName,
                    StoragePath = relativePath,
                    UploadedAtUtc = DateTime.UtcNow,
                    UploadedByUserId = currentUserId,
                    FileSizeBytes = request.FileSizeBytes,
                    ContentType = request.ContentType
                };

                _context.Documents.Add(document);
            }

            if (_context.Database.IsRelational())
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            foreach (var result in results)
            {
                result.Status = "Uploaded";
                result.Persisted = true;
            }

            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document batch upload failed. Cleaning up saved files.");

            foreach (var savedPath in savedPaths)
            {
                try
                {
                    await _fileStorageService.DeleteAsync(savedPath, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to delete saved file {SavedPath} during upload cleanup.", savedPath);
                }
            }

            foreach (var result in results)
            {
                result.Status = "Rejected";
                result.ValidationErrors.Add("Upload failed and the batch was rolled back. " + ex.Message);
                result.Persisted = false;
            }

            return results.ToArray();
        }
    }

    public async Task<DocumentContentResult> GetAuthorizedDocumentContentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException($"Document {documentId} was not found.");
        }

        if (!document.UploadedByUserId.Equals(currentUserId))
        {
            throw new UnauthorizedAccessException("You do not have permission to access this document.");
        }

        var contentStream = await _fileStorageService.OpenReadAsync(document.StoragePath, cancellationToken);
        return new DocumentContentResult
        {
            Document = document,
            Content = contentStream,
            ContentType = document.ContentType
        };
    }

    private int GetCurrentUserId()
    {
        var contextUser = _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
        var userId = _documentAuthorizationService.GetCurrentUserId(contextUser);

        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("The current user is not authenticated.");
        }

        return userId.Value;
    }
}

