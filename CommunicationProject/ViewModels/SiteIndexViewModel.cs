using CommunicationProject.Models;

namespace CommunicationProject.ViewModels
{
    public class SiteIndexViewModel
    {
        public List<Site> Sites { get; set; } = new();

        public string? Search { get; set; }
        public string? Location { get; set; }

        public List<string> Locations { get; set; } = new();

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}