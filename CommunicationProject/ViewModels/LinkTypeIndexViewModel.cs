using CommunicationProject.Models;

namespace CommunicationProject.ViewModels;

public class LinkTypeIndexViewModel
{
    public List<LinkTypeListItemViewModel> LinkTypes { get; set; } = new();

    public string? Search { get; set; }

    public string? UsageStatus { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; } = 10;

    public int TotalPages { get; set; }

    public int TotalItems { get; set; }

    public bool HasPreviousPage =>
        PageNumber > 1;

    public bool HasNextPage =>
        PageNumber < TotalPages;

    public int FirstItem =>
        TotalItems == 0
            ? 0
            : ((PageNumber - 1) * PageSize) + 1;

    public int LastItem =>
        Math.Min(
            PageNumber * PageSize,
            TotalItems);
}


public class LinkTypeListItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int LinkCount { get; set; }
}