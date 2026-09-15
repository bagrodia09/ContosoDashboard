using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<DocumentStorageOptions> options, IWebHostEnvironment environment)
    {
        var configuredRoot = options.Value.RootPath;
        var rootPath = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(environment.ContentRootPath, "AppData", "uploads")
            : configuredRoot;

        _rootPath = Path.IsPathRooted(rootPath)
            ? Path.GetFullPath(rootPath)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, rootPath));

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : Path.GetFullPath(environment.WebRootPath);
        var webRootWithSeparator = EnsureTrailingSeparator(Path.GetFullPath(webRoot));
        var rootWithSeparator = EnsureTrailingSeparator(_rootPath);
        if (rootWithSeparator.StartsWith(webRootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Document storage must be outside wwwroot.");
        }

        Directory.CreateDirectory(_rootPath);
    }

    public string ResolveRootPath() => _rootPath;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(fileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
        var relativePath = Path.Combine("documents", $"{Guid.NewGuid():N}{safeExtension}").Replace('\\', '/');
        var targetPath = GetAbsolutePath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await using var writeStream = File.Create(targetPath);
        content.Position = 0;
        await content.CopyToAsync(writeStream, cancellationToken);

        return relativePath;
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        return Task.FromResult<Stream>(File.OpenRead(absolutePath));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string GetAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path cannot be null or empty.", nameof(relativePath));
        }

        var normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(_rootPath, normalizedRelative));
        var rootPath = Path.GetFullPath(_rootPath);

        var rootWithSeparator = EnsureTrailingSeparator(rootPath);
        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage path is not contained within the configured storage root.");
        }

        return combined;
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
