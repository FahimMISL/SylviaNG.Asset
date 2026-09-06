namespace RMS.Application.Features.Requisitions;

/// <summary>US-007: minimum supported types and default size limits (configurable via
/// Rms:MaxAttachmentSizeMb / Rms:MaxTotalAttachmentsSizeMb in appsettings).</summary>
public static class RequisitionAttachmentRules
{
    public const long DefaultMaxFileSizeMb = 10;
    public const long DefaultMaxTotalSizeMb = 50;

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".xlsx", ".jpg", ".jpeg", ".png", ".pptx",
    };

    public static bool IsAllowedExtension(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName));
}
