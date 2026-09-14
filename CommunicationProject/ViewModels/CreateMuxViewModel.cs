using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateMuxViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Communication Link")]
    public Guid? CommunicationLinkId { get; set; }

    [Required]
    [Display(Name = "Site")]
    public string SiteId { get; set; } =
        string.Empty;

    public List<SelectListItem> CommunicationLinkOptions { get; set; } =
        new();

    public List<SelectListItem> SiteOptions { get; set; } =
        new();
}