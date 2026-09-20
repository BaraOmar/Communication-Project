using CommunicationProject.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public sealed class CommunicationLinkCreateViewModel
    : IValidatableObject
{
    [Required]
    [Display(Name = "Link Technology")]
    public LinkTechnology? Technology { get; set; }


    [Required(ErrorMessage = "Source site is required.")]
    [Display(Name = "Site From")]
    public string SiteFromId { get; set; } = string.Empty;


    [Required(ErrorMessage = "Destination site is required.")]
    [Display(Name = "Site To")]
    public string SiteToId { get; set; } = string.Empty;


    [Display(Name = "Number of SDH Cards")]
    public int? SdhCardCount { get; set; }


    [Display(Name = "Number of STMs")]
    public int? StmCount { get; set; }


    [Display(Name = "Number of E1s")]
    public int? PdhE1Count { get; set; }

    public bool LockSiteFrom { get; set; }


    [ValidateNever]
    public List<SelectListItem> SiteOptions
    { get; set; } = new();


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


        if (Technology == LinkTechnology.SDH)
        {
            if (!SdhCardCount.HasValue ||
                !new[] { 2, 6, 20 }
                    .Contains(SdhCardCount.Value))
            {
                yield return new ValidationResult(
                    "Select 2, 6, or 20 SDH cards.",
                    new[] { nameof(SdhCardCount) });
            }

            if (!StmCount.HasValue ||
                StmCount.Value < 1 ||
                StmCount.Value > 9)
            {
                yield return new ValidationResult(
                    "STM count must be between 1 and 9.",
                    new[] { nameof(StmCount) });
            }
        }


        if (Technology == LinkTechnology.PDH)
        {
            if (!PdhE1Count.HasValue ||
                PdhE1Count.Value < 1 ||
                PdhE1Count.Value > 1000)
            {
                yield return new ValidationResult(
                    "PDH E1 count must be between 1 and 1000.",
                    new[] { nameof(PdhE1Count) });
            }
        }
    }
}