using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models;

public class Customer
{
    [Key]
    public Guid Id { get; set; } =
        Guid.NewGuid();

    [Required]
    [StringLength(
        200,
        ErrorMessage = "Customer name cannot exceed 200 characters.")]
    [Display(Name = "Customer Name")]
    public string Name { get; set; } =
        string.Empty;

    [StringLength(
        500,
        ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } =
        true;
    public ICollection<CustomerConnection> Connections { get; set; } =
    new List<CustomerConnection>();
}