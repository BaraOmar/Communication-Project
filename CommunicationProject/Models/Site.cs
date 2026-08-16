using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

[Index(nameof(Name), IsUnique = true)]
public class Site
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Required(ErrorMessage = "Site ID is required.")]
    [StringLength(
        100,
        ErrorMessage = "Site ID cannot exceed 100 characters.")]
    [Display(Name = "Site ID")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Site name is required.")]
    [StringLength(
        100,
        ErrorMessage = "Site name cannot exceed 100 characters.")]
    [RegularExpression(
        @".*\S.*",
        ErrorMessage = "Site name cannot contain only spaces.")]
    [Display(Name = "Site Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(
        255,
        ErrorMessage = "Location cannot exceed 255 characters.")]
    [RegularExpression(
        @".*\S.*",
        ErrorMessage = "Location cannot contain only spaces.")]
    public string Location { get; set; } = string.Empty;

    [InverseProperty(nameof(CommunicationLink.SiteFrom))]
    public ICollection<CommunicationLink> LinksFrom { get; set; }
        = new List<CommunicationLink>();

    [InverseProperty(nameof(CommunicationLink.SiteTo))]
    public ICollection<CommunicationLink> LinksTo { get; set; }
        = new List<CommunicationLink>();
}