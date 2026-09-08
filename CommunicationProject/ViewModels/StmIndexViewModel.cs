using CommunicationProject.Models;

namespace CommunicationProject.ViewModels
{
    public class StmIndexViewModel
    {
        public List<Stm> Stms { get; set; } = new();

        public string? Search { get; set; }
        public string? SiteId { get; set; }
        public string? ConnectionStatus { get; set; }

        public List<Site> Sites { get; set; } = new();

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}