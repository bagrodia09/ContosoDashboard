using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ContosoDashboard.Services;

namespace ContosoDashboard.Data;

public class DocumentSchemaInitializer
{
    private const string SchemaVersion = "001-document-upload-management";
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentSchemaInitializer> _logger;
    private readonly DocumentStorageOptions _storageOptions;

    public DocumentSchemaInitializer(
        ApplicationDbContext context,
        IOptions<DocumentStorageOptions> storageOptions,
        ILogger<DocumentSchemaInitializer> logger)
    {
        _context = context;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_context.Database.IsRelational())
            {
                await _context.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }

            // A brand-new LocalDB database has no catalog to upgrade. Let EF
            // create the complete current model (including seed data) first.
            if (!await _context.Database.CanConnectAsync(cancellationToken))
            {
                await _context.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }

            var usersTableCount = await _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'")
                .SingleAsync(cancellationToken);
            if (usersTableCount == 0)
            {
                throw new InvalidOperationException(
                    "The existing database does not contain the expected Users table; schema upgrade was not attempted.");
            }

            var documentsTableCount = await _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Documents'")
                .SingleAsync(cancellationToken);

            if (documentsTableCount == 0)
            {
                await CreateBackupAsync(cancellationToken);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            if (documentsTableCount == 0)
            {
                await _context.Database.ExecuteSqlRawAsync(@"CREATE TABLE [Documents] (
                    [DocumentId] int NOT NULL IDENTITY(1,1),
                    [Title] nvarchar(200) NOT NULL,
                    [Description] nvarchar(2000) NULL,
                    [Category] nvarchar(255) NOT NULL,
                    [Tags] nvarchar(1000) NULL,
                    [OriginalFileName] nvarchar(255) NOT NULL,
                    [StoragePath] nvarchar(500) NOT NULL,
                    [UploadedAtUtc] datetime2 NOT NULL,
                    [UploadedByUserId] int NOT NULL,
                    [FileSizeBytes] bigint NOT NULL,
                    [ContentType] nvarchar(255) NOT NULL,
                    CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]),
                    CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                );");

                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX [IX_Documents_UploadedByUserId] ON [Documents] ([UploadedByUserId]);");
                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX [IX_Documents_Category] ON [Documents] ([Category]);");
                await _context.Database.ExecuteSqlRawAsync("CREATE INDEX [IX_Documents_UploadedAtUtc] ON [Documents] ([UploadedAtUtc]);");
            }

            var versionTableCount = await _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DocumentSchemaVersions'")
                .SingleAsync(cancellationToken);

            if (versionTableCount == 0)
            {
                await _context.Database.ExecuteSqlRawAsync(@"CREATE TABLE [DocumentSchemaVersions] (
                    [SchemaName] nvarchar(128) NOT NULL,
                    [AppliedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_DocumentSchemaVersions] PRIMARY KEY ([SchemaName])
                );");
            }

            var versionExists = await _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [DocumentSchemaVersions] WHERE [SchemaName] = {0}", SchemaVersion)
                .SingleAsync(cancellationToken);

            if (versionExists == 0)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO [DocumentSchemaVersions] ([SchemaName], [AppliedAtUtc]) VALUES ({SchemaVersion}, {DateTime.UtcNow})", cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The document schema initialization failed.");
            throw;
        }
    }

    private async Task CreateBackupAsync(CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var databaseName = connection.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The LocalDB database name is unavailable; schema backup was not attempted.");
        }

        var backupDirectory = Path.IsPathRooted(_storageOptions.BackupPath)
            ? _storageOptions.BackupPath
            : Path.Combine(AppContext.BaseDirectory, _storageOptions.BackupPath);
        Directory.CreateDirectory(backupDirectory);

        var backupFile = Path.Combine(
            backupDirectory,
            $"{databaseName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak");
        var escapedDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        var escapedBackupFile = backupFile.Replace("'", "''", StringComparison.Ordinal);
        await _context.Database.ExecuteSqlRawAsync(
            $"BACKUP DATABASE [{escapedDatabaseName}] TO DISK = N'{escapedBackupFile}' WITH INIT",
            cancellationToken);

        if (!File.Exists(backupFile))
        {
            throw new IOException($"The database backup was not created at '{backupFile}'.");
        }

        _logger.LogInformation("Created LocalDB schema-upgrade backup at {BackupFile}.", backupFile);
    }
}
