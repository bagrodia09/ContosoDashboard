namespace ContosoDashboard.Services;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; set; } = "AppData/uploads";
    public string BackupPath { get; set; } = "AppData/backups";
    public long MaxFileSizeBytes { get; set; } = 26_214_400;
}
