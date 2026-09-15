using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
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

namespace ContosoDashboard.Tests;

public class DocumentValidationServiceTests
{
    [Fact]
    public void Validate_WhenDocumentIsValid_ReturnsTrue()
    {
        var validation = new DocumentValidationService();
        var request = new DocumentUploadRequest
        {
            FileName = "sample.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2_048,
            Title = "Quarterly report",
            Category = "Reports",
            Description = "Q4 summary",
            Tags = "finance, summary",
            FileStream = new MemoryStream(new byte[2_048])
        };

        var result = validation.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("sample.pdf", "application/pdf", "Reports", true)]
    [InlineData("sample.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Personal Files", true)]
    [InlineData("sample.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Team Resources", true)]
    [InlineData("sample.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "Presentations", true)]
    [InlineData("sample.txt", "text/plain", "Other", true)]
    [InlineData("sample.jpg", "image/jpeg", "Project Documents", true)]
    [InlineData("sample.jpeg", "image/jpeg", "Project Documents", true)]
    [InlineData("sample.png", "image/png", "Personal Files", true)]
    public void Validate_WhenMimeMatchesAndCategoryIsAllowed_ReturnsValid(string fileName, string contentType, string category, bool expected)
    {
        var validation = new DocumentValidationService();
        var request = new DocumentUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = 512,
            Title = "Test title",
            Category = category,
            Description = "A valid file.",
            Tags = "test",
            FileStream = new MemoryStream(new byte[512])
        };

        var result = validation.Validate(request);

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void Validate_WhenMimeMismatchOrOversizedFile_RejectsRequest()
    {
        var validation = new DocumentValidationService();

        var mismatched = new DocumentUploadRequest
        {
            FileName = "sample.pdf",
            ContentType = "image/png",
            FileSizeBytes = 512,
            Title = "Wrong mime",
            Category = "Personal Files",
            Description = "Mismatch",
            FileStream = new MemoryStream(new byte[512])
        };

        var oversized = new DocumentUploadRequest
        {
            FileName = "sample.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 26_214_401,
            Title = "Too large",
            Category = "Reports",
            Description = "Oversized",
            FileStream = new MemoryStream(new byte[26_214_401])
        };

        Assert.False(validation.Validate(mismatched).IsValid);
        Assert.False(validation.Validate(oversized).IsValid);
    }
}

public class DocumentServiceTests
{
    [Fact]
    public async Task UploadBatchAsync_WhenOwnerUploadsValidFiles_PersistsDocuments()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.Users.AddRange(
            new User { UserId = 1, Email = "owner@example.com", DisplayName = "Owner", Role = UserRole.Employee },
            new User { UserId = 2, Email = "other@example.com", DisplayName = "Other", Role = UserRole.Employee });
        await db.SaveChangesAsync();

        var storage = new LocalFileStorageService(
            Options.Create(new DocumentStorageOptions { RootPath = Path.Combine(Path.GetTempPath(), "contoso-documents-tests", Guid.NewGuid().ToString()) }),
            new TestWebHostEnvironment());
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUser(1)
            }
        };

        var service = new DocumentService(
            db,
            storage,
            new DocumentValidationService(),
            new DocumentAuthorizationService(),
            httpContextAccessor,
            new LoggerFactory().CreateLogger<DocumentService>());

        var request = new DocumentUploadRequest
        {
            FileName = "sample.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 512,
            Title = "Quarterly report",
            Category = "Reports",
            Description = "Ready to store",
            Tags = "finance",
            FileStream = new MemoryStream(new byte[512])
        };

        var results = await service.UploadBatchAsync(new[] { request });

        Assert.Single(results);
        Assert.Equal("Uploaded", results[0].Status);
        Assert.True(results[0].Persisted);
        Assert.Equal(1, await db.Documents.CountAsync());
    }

    [Fact]
    public async Task GetOwnedDocumentsAsync_WhenUserIsOwner_OnlyOwnDocumentsAreReturned()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.Users.AddRange(
            new User { UserId = 1, Email = "owner@example.com", DisplayName = "Owner", Role = UserRole.Employee },
            new User { UserId = 2, Email = "other@example.com", DisplayName = "Other", Role = UserRole.Employee });

        db.Documents.AddRange(
            new Document { DocumentId = 1, Title = "Mine", Category = "Personal Files", OriginalFileName = "mine.pdf", StoragePath = "documents/mine.pdf", UploadedByUserId = 1, UploadedAtUtc = DateTime.UtcNow, FileSizeBytes = 128, ContentType = "application/pdf" },
            new Document { DocumentId = 2, Title = "Not mine", Category = "Reports", OriginalFileName = "other.pdf", StoragePath = "documents/other.pdf", UploadedByUserId = 2, UploadedAtUtc = DateTime.UtcNow, FileSizeBytes = 128, ContentType = "application/pdf" });
        await db.SaveChangesAsync();

        var storage = new LocalFileStorageService(
            Options.Create(new DocumentStorageOptions { RootPath = Path.Combine(Path.GetTempPath(), "contoso-documents-tests", Guid.NewGuid().ToString()) }),
            new TestWebHostEnvironment());

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUser(1)
            }
        };

        var service = new DocumentService(
            db,
            storage,
            new DocumentValidationService(),
            new DocumentAuthorizationService(),
            httpContextAccessor,
            new LoggerFactory().CreateLogger<DocumentService>());

        var documents = await service.GetOwnedDocumentsAsync();

        Assert.Single(documents);
        Assert.Equal("Mine", documents[0].Title);
    }

    [Fact]
    public async Task GetAuthorizedDocumentContentAsync_WhenUserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.Users.Add(new User { UserId = 1, Email = "owner@example.com", DisplayName = "Owner", Role = UserRole.Employee });
        db.Documents.Add(new Document
        {
            DocumentId = 1,
            Title = "Secret",
            Category = "Personal Files",
            OriginalFileName = "secret.pdf",
            StoragePath = "documents/secret.pdf",
            UploadedByUserId = 1,
            UploadedAtUtc = DateTime.UtcNow,
            FileSizeBytes = 128,
            ContentType = "application/pdf"
        });
        await db.SaveChangesAsync();

        var tempRoot = Path.Combine(Path.GetTempPath(), "contoso-documents-tests", Guid.NewGuid().ToString());
        var storage = new LocalFileStorageService(
            Options.Create(new DocumentStorageOptions { RootPath = tempRoot }),
            new TestWebHostEnvironment { ContentRootPath = tempRoot, WebRootPath = Path.Combine(tempRoot, "wwwroot") });

        await storage.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3 }), "secret.pdf");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUser(2)
            }
        };

        var service = new DocumentService(
            db,
            storage,
            new DocumentValidationService(),
            new DocumentAuthorizationService(),
            httpContextAccessor,
            new LoggerFactory().CreateLogger<DocumentService>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAuthorizedDocumentContentAsync(1));
    }

    [Fact]
    public void LocalStorageService_StoresFilesOutsideWwwroot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "contoso-documents-tests", Guid.NewGuid().ToString());
        var env = new TestWebHostEnvironment
        {
            ContentRootPath = tempRoot,
            WebRootPath = Path.Combine(tempRoot, "wwwroot")
        };

        var service = new LocalFileStorageService(
            Options.Create(new DocumentStorageOptions { RootPath = Path.Combine(tempRoot, "AppData", "uploads") }),
            env);

        Assert.DoesNotContain("wwwroot", service.ResolveRootPath(), StringComparison.OrdinalIgnoreCase);
        Assert.True(Directory.Exists(service.ResolveRootPath()));
    }

    [Fact]
    public void AuthenticationGuardrail_UsesCookieAuthenticationOnly()
    {
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        var registrations = services.Where(s => s.ServiceType.FullName?.Contains("OpenIdConnect", StringComparison.OrdinalIgnoreCase) == true || s.ServiceType.FullName?.Contains("MicrosoftIdentity", StringComparison.OrdinalIgnoreCase) == true).ToList();

        Assert.Empty(registrations);
    }

    private static ClaimsPrincipal CreateUser(int userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Test user")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ContosoDashboard.Tests";
        public string EnvironmentName { get; set; } = Environments.Development;
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "contoso-documents-tests");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
