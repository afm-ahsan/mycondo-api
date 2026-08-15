using System.ComponentModel.DataAnnotations;

namespace MyCondo.Application.Common.Settings;

public sealed record StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Directory uploaded files are written under, resolved relative to the API host's content root
    /// when not rooted. Local-disk storage is an accepted MVP-1 limitation (see IFileStorageService) —
    /// this is not durable across container redeploys and must be revisited before a multi-instance
    /// production deployment.
    /// </summary>
    [Required] public string RootPath { get; init; } = "App_Data/uploads";
}
