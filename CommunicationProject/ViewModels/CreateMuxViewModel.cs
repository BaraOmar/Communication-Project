using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateMuxViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "MUX Name")]
    public string Name { get; set; } = string.Empty;


    [Required]
    [Display(Name = "Site")]
    public string SiteId { get; set; } = string.Empty;


    [Required]
    [Display(Name = "MUX Type")]
    public Guid? MuxTypeId { get; set; }


    public List<SelectListItem> SiteOptions
    { get; set; } = new();

    public List<SelectListItem> MuxTypeOptions
    { get; set; } = new();
}