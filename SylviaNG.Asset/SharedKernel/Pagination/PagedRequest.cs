using System.ComponentModel.DataAnnotations;

namespace SylviaNG.Assets.SharedKernel.Pagination
{
    public class PagedRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; }

        public string? SearchTerm { get; set; }

        public string[]? SearchProperties { get; set; }
    }

    public enum SortDirection
    {
        Asc = 0,
        Desc = 1
    }
}
