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
            new List<SelectListItem>();

        public List<SelectListItem> SiteOptions { get; set; } =
            new List<SelectListItem>();

        public List<PathSearchResultViewModel> Results { get; set; } =
            new List<PathSearchResultViewModel>();
    }
}