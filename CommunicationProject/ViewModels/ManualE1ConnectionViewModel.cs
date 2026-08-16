using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public sealed class ManualE1ConnectionViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Source site is required.")]
    [Display(Name = "Site From")]
    public string SiteFromId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Source STM is required.")]
    [Display(Name = "STM From")]
    public Guid? FromStmId { get; set; }

    [Required(ErrorMessage = "Source E1 is required.")]
    [Display(Name = "E1 From")]
    public Guid? FromE1Id { get; set; }

    [Required(ErrorMessage = "Destination site is required.")]
    [Display(Name = "Site To")]
    public string SiteToId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Destination STM is required.")]
    [Display(Name = "STM To")]
    public Guid? ToStmId { get; set; }

    [Required(ErrorMessage = "Destination E1 is required.")]
    [Display(Name = "E1 To")]
    public Guid? ToE1Id { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    // Dropdown options — not database fields.

    [ValidateNever]
    public List<SelectListItem> SiteOptions { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> FromStmOptions { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> FromE1Options { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> ToStmOptions { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> ToE1Options { get; set; } = [];

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
                "Source site and destination site cannot be the same.",
                new[] { nameof(SiteToId) });
        }

        if (FromE1Id.HasValue &&
            ToE1Id.HasValue &&
            FromE1Id.Value == ToE1Id.Value)
        {
            yield return new ValidationResult(
                "An E1 channel cannot be connected to itself.",
                new[] { nameof(ToE1Id) });
        }
    }
}