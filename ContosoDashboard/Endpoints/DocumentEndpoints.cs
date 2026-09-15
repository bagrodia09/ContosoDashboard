using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/documents/upload", async (HttpContext context, IDocumentService documentService) =>
        {
            var form = await context.Request.ReadFormAsync();
            var files = form.Files;
            if (files.Count == 0)
            {
                return Results.BadRequest(new[]
                {
                    new DocumentUploadResult
                    {
                        DisplayFileName = string.Empty,
                        Status = "Rejected",
                        ValidationErrors = new List<string> { "No files were selected for upload." },
                        Persisted = false
                    }
                });
            }

            var uploadRequests = new List<DocumentUploadRequest>();
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var title = ReadFirstValue(form, new[] { $"metadata[{i}][title]", $"metadata[{i}].title", $"title[{i}]", "title" });
                var category = ReadFirstValue(form, new[] { $"metadata[{i}][category]", $"metadata[{i}].category", $"category[{i}]", "category" });
                var description = ReadFirstValue(form, new[] { $"metadata[{i}][description]", $"metadata[{i}].description", $"description[{i}]", "description" });
                var tags = ReadFirstValue(form, new[] { $"metadata[{i}][tags]", $"metadata[{i}].tags", $"tags[{i}]", "tags" });

                uploadRequests.Add(new DocumentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    FileStream = file.OpenReadStream(),
                    Title = title ?? string.Empty,
                    Category = category ?? string.Empty,
                    Description = description,
                    Tags = tags
                });
            }

            var results = await documentService.UploadBatchAsync(uploadRequests);
            return results.Any(r => r.ValidationErrors.Count > 0 || r.Status == "ValidatedNotPersisted")
                ? Results.BadRequest(results)
                : Results.Ok(results);
        }).RequireAuthorization();

        endpoints.MapGet("/api/documents/{documentId:int}/content", async (int documentId, IDocumentService documentService) =>
        {
            try
            {
                var content = await documentService.GetAuthorizedDocumentContentAsync(documentId);
                return Results.File(content.Content, content.ContentType, enableRangeProcessing: true);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        }).RequireAuthorization();
    }

    private static string? ReadFirstValue(IFormCollection form, IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var candidate = form[key];
            if (candidate.Count > 0)
            {
                return candidate[0];
            }
        }

        return null;
    }
}
