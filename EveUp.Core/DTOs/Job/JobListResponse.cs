namespace EveUp.Core.DTOs.Job;

public class JobListResponse
{
    public List<JobResponse> Data { get; set; } = new();
    public PaginationMeta Meta { get; set; } = new();
}

public class PaginationMeta
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
}
