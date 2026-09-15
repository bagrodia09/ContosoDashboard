namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<ContosoDashboard.Models.Document>> GetOwnedDocumentsAsync();
    Task<ContosoDashboard.Models.DocumentUploadResult[]> UploadBatchAsync(IEnumerable<ContosoDashboard.Models.DocumentUploadRequest> requests, CancellationToken cancellationToken = default);
    Task<ContosoDashboard.Models.DocumentContentResult> GetAuthorizedDocumentContentAsync(int documentId, CancellationToken cancellationToken = default);
}
