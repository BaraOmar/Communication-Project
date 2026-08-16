using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public sealed class CreateE1InventoryViewModel
{
    [Required]
    public Guid LinkId { get; set; }

    [Required(ErrorMessage = "Site is required.")]
    [Display(Name = "Site")]
    public string SiteId { get; set; } = string.Empty;

    [Required(ErrorMessage = "STM is required.")]
    [Display(Name = "STM")]
    public Guid? StmId { get; set; }

    [ValidateNever]
    public string LinkName { get; set; } = string.Empty;

    [ValidateNever]
    public List<SelectListItem> SiteOptions { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> StmOptions { get; set; } = [];
}