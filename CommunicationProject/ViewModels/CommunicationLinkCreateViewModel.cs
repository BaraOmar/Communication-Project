using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public sealed class CommunicationLinkCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Link name is required.")]
    [StringLength(150)]
    [Display(Name = "Link Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Link type is required.")]
    [Display(Name = "Link Type")]
    public Guid? LinkTypeId { get; set; }

    [Required(ErrorMessage = "Source site is required.")]
    [Display(Name = "Site From")]
    public string SiteFromId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Destination site is required.")]
    [Display(Name = "Site To")]
    public string SiteToId { get; set; } = string.Empty;

    [Required(ErrorMessage = "STM count is required.")]
    [Range(1, 100, ErrorMessage = "STM count must be between 1 and 100.")]
    [Display(Name = "Number of STM Pairs")]
    public int StmCount { get; set; } = 1;

    // These lists are used only to build the form.
    [ValidateNever]
    public List<SelectListItem> LinkTypeOptions { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> SiteOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(SiteFromId) &&
            !string.IsNullOrWhiteSpace(SiteToId) &&
            string.Equals(
                SiteFromId,
                SiteToId,
                StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "A communication link cannot connect a site to itself.",
                new[] { nameof(SiteToId) });
        }
    }
}