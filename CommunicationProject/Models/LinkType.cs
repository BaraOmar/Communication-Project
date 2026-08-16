using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models;

[Index(nameof(Name), IsUnique = true)]
public class LinkType
{
    [Key]
    [Display(Name = "Type ID")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Link type name is required.")]
    [StringLength(
        100,
        ErrorMessage = "Link type name cannot exceed 100 characters.")]
    [RegularExpression(
        @".*\S.*",
        ErrorMessage = "Link type name cannot contain only spaces.")]
    [Display(Name = "Link Type")]
    public string Name { get; set; } = string.Empty;

    public ICollection<CommunicationLink> Links { get; set; }
        = new List<CommunicationLink>();
}