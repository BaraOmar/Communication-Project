using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public sealed class CreateE1ConnectionViewModel : IValidatableObject
{
    public Guid LinkId { get; set; }

    [Required(ErrorMessage = "Source STM is required.")]
    [Display(Name = "STM From")]
    public Guid? FromStmId { get; set; }

    [Required(ErrorMessage = "Source E1 is required.")]
    [Display(Name = "Available E1 From")]
    public Guid? FromE1Id { get; set; }

    [Required(ErrorMessage = "Destination STM is required.")]
    [Display(Name = "STM To")]
    public Guid? ToStmId { get; set; }

    [Required(ErrorMessage = "Destination E1 is required.")]
    [Display(Name = "Available E1 To")]
    public Guid? ToE1Id { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    // Display-only information.

    [ValidateNever]
    public string LinkName { get; set; } = string.Empty;

    [ValidateNever]
    public string SiteFromName { get; set; } = string.Empty;

    [ValidateNever]
    public string SiteToName { get; set; } = string.Empty;

    // Dropdown options.

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
        if (LinkId == Guid.Empty)
        {
            yield return new ValidationResult(
                "The communication link is invalid.",
                new[] { nameof(LinkId) });
        }

        if (FromStmId.HasValue &&
            ToStmId.HasValue &&
            FromStmId.Value == ToStmId.Value)
        {
            yield return new ValidationResult(
                "The source STM and destination STM cannot be the same.",
                new[] { nameof(ToStmId) });
        }

        if (FromE1Id.HasValue &&
            ToE1Id.HasValue &&
            FromE1Id.Value == ToE1Id.Value)
        {
            yield return new ValidationResult(
                "An E1 channel cannot connect to itself.",
                new[] { nameof(ToE1Id) });
        }
    }
}