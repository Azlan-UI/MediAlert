namespace MediAlert.DTOs.Common;

public class PagedSortRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool Desc { get; set; }
}
