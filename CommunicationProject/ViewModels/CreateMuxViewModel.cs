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

    [Range(
    1,
    100,
    ErrorMessage = "Number of shelves must be at least 1.")]
    [Display(Name = "Number of Shelves")]
    public int? ShelfCount { get; set; }


    [Required]
    [Range(
        1,
        1000,
        ErrorMessage = "Card slot count must be at least 1.")]
    [Display(Name = "Card Slots")]
    public int? CardSlotCount { get; set; }

    public List<SelectListItem> SiteOptions { get; set; }
        = new();

    public List<SelectListItem> MuxTypeOptions { get; set; }
        = new();

    public Dictionary<Guid, bool> MuxTypeShelfSupport { get; set; }
    = new();
}