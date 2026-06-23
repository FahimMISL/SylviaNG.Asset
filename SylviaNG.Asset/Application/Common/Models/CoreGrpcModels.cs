namespace SylviaNG.Assets.Application.Common.Models
{
    public class EntityIdNameCodeResponse
    {
        public long EntityId { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }

        public string CodeName =>
            (!string.IsNullOrWhiteSpace(Code) && !string.IsNullOrWhiteSpace(Name))
                ? $"{Code} - {Name}"
                : Name ?? string.Empty;
    }

    public class CoreBatchLookupResult
    {
        public List<EntityIdNameCodeResponse> Sites { get; set; } = new();
    }
}
