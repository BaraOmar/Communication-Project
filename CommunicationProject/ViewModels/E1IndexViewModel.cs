using CommunicationProject.Models;

namespace CommunicationProject.ViewModels
{
    public class E1IndexViewModel
    {
        public List<E1> E1Channels { get; set; } = new();

        public string? Search { get; set; }
        public string? SiteFromId { get; set; }
        public string? SiteToId { get; set; }
        public string? Description { get; set; }
        public string? ConnectionStatus { get; set; }
        public string? State { get; set; }
        public string? OperationalStatus { get; set; }
        public string? ConnectionType { get; set; }
        public List<Site> Sites { get; set; } = new();

        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 20;
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
}