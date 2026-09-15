using System.Security.Claims;

namespace ContosoDashboard.Services;

public interface IDocumentAuthorizationService
{
    int? GetCurrentUserId(ClaimsPrincipal user);
    bool CanAccessOwnedDocument(int uploadedByUserId, ClaimsPrincipal user);
}

public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    public int? GetCurrentUserId(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    public bool CanAccessOwnedDocument(int uploadedByUserId, ClaimsPrincipal user)
    {
        var currentUserId = GetCurrentUserId(user);
        return currentUserId.HasValue && currentUserId.Value == uploadedByUserId;
    }
}
