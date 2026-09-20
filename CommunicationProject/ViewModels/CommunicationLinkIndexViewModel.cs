using CommunicationProject.Models;

namespace CommunicationProject.ViewModels;

public sealed class CommunicationLinkIndexViewModel
{
    public List<CommunicationLinkListItemViewModel> Links { get; set; } = [];

    public string? Search { get; set; }

    public Guid? LinkTypeId { get; set; }

    public string? SiteId { get; set; }

    public string? InventoryStatus { get; set; }

    public List<LinkType> LinkTypes { get; set; } = [];

    public List<Site> Sites { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; } = 15;

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


public sealed class CommunicationLinkListItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string LinkTypeName { get; set; } = string.Empty;

    public string SiteFromId { get; set; } = string.Empty;

    public string SiteFromName { get; set; } = string.Empty;

    public string SiteToId { get; set; } = string.Empty;

    public string SiteToName { get; set; } = string.Empty;

    public string? Capacity { get; set; }

    public int StmCount { get; set; }

    public int ReverseStmCount { get; set; }

    public int SdhCardCount { get; set; }

    public int ReverseSdhCardCount { get; set; }

    public int E1Count { get; set; }

    public int ReverseE1Count { get; set; }

    public bool IsSdh =>
        string.Equals(
            LinkTypeName,
            "SDH",
            StringComparison.OrdinalIgnoreCase);

    public bool IsPdh =>
        string.Equals(
            LinkTypeName,
            "PDH",
            StringComparison.OrdinalIgnoreCase);
}