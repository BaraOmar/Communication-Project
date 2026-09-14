using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models;

public class CommunicationPath
{
    [Key]
    public Guid Id { get; set; } =
        Guid.NewGuid();

    [Required]
    [StringLength(
        200,
        ErrorMessage = "Path name cannot exceed 200 characters.")]
    [Display(Name = "Path Name")]
    public string Name { get; set; } =
        string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } =
        true;

    public DateTime CreatedAt { get; set; } =
        DateTime.Now;

    public ICollection<CommunicationPathSegment> Segments { get; set; } =
    new List<CommunicationPathSegment>();
    public ICollection<CustomerConnection> CustomerConnections { get; set; } =
    new List<CustomerConnection>();
}