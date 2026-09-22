using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.ViewModels
{
    public class SearchPathViewModel
    {
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Link")]
        public string? LinkName { get; set; }

        [Display(Name = "Site")]
        public string? SiteId { get; set; }

        [Display(Name = "Site Position")]
        public string SitePosition { get; set; } = "anywhere";


        public List<SelectListItem> LinkOptions { get; set; } =
            new();

        public List<SelectListItem> SiteOptions { get; set; } =
            new();

        public List<PathSearchResultViewModel> Results { get; set; } =
            new();


        // Pagination
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }


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

        public List<CustomerPathUsageViewModel> Customers { get; set; } =
    new();
    }
    public class CustomerPathUsageViewModel
    {
        public Guid CustomerConnectionId { get; set; }
        public Guid? ManageE1Id { get; set; }

        public string CustomerName { get; set; } =
            string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public E1OperationalStatus? OperationalStatus { get; set; }

        public List<CustomerPathE1ViewModel> E1s { get; set; } =
            new();

        public string StartSiteId { get; set; } =
    string.Empty;

        public string DestinationSiteId { get; set; } =
            string.Empty;
    }


    public class CustomerPathE1ViewModel
    {
        public Guid CustomerConnectionSegmentId { get; set; }

        public Guid E1Id { get; set; }

        public int SegmentOrder { get; set; }

        public string SiteFrom { get; set; } =
            string.Empty;

        public string SiteTo { get; set; } =
            string.Empty;

        public string StmNumber { get; set; } =
            string.Empty;
        public string E1Number { get; set; } =
            string.Empty;
    }
}