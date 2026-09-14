using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommunicationProject.ViewModels;

public class AddCustomerToPathViewModel
{
    public Guid PathId { get; set; }

    public string PathDisplay { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Customer")]
    public string CustomerName { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Start Site")]
    public string StartSiteId { get; set; } =
    string.Empty;

    [Required]
    [Display(Name = "Destination Site")]
    public string DestinationSiteId { get; set; } =
        string.Empty;

    public List<SelectListItem> PathSites { get; set; } =
        new();

    public List<CustomerPathSegmentInputViewModel> Segments { get; set; } =
        new();
}


public class CustomerPathSegmentInputViewModel
{
    public Guid CommunicationPathSegmentId { get; set; }

    public int Order { get; set; }

    public string SiteFrom { get; set; } =
        string.Empty;

    public string SiteTo { get; set; } =
        string.Empty;

    [Display(Name = "E1")]
    public Guid? E1Id { get; set; }

    public List<SelectListItem> AvailableE1s { get; set; } =
        new();
}