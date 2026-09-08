using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels
{
    public class SearchPathViewModel
    {
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Link")]
        public string? LinkName { get; set; }

        [Display(Name = "Source Site")]
        public string? SourceSiteId { get; set; }


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
    }
}