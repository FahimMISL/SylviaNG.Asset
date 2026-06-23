namespace SylviaNG.Assets.Application.Features.Assets.Models
{
    public class AssetLookupResponse
    {
        public long AssetId { get; set; }
        public string? AssetCode { get; set; }
        public string? Name { get; set; }

        public string CodeName =>
            (!string.IsNullOrWhiteSpace(Name))
                ? $"{AssetCode} - {Name}"
                : Name ?? string.Empty;
    }
}