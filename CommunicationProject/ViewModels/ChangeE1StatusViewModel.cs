using CommunicationProject.Models;
using System.ComponentModel.DataAnnotations;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.ViewModels;

public class ChangeE1StatusViewModel
{
    [Required]
    public Guid E1Id { get; set; }

    [Display(Name = "E1")]
    public string E1Number { get; set; } =
        string.Empty;

    [Display(Name = "Connection Group")]
    public Guid? ConnectionGroupId { get; set; }

    public string CustomerName { get; set; } =
    string.Empty;

    [Display(Name = "Current Status")]
    public E1OperationalStatus CurrentStatus { get; set; }

    [Required]
    [Display(Name = "New Status")]
    public E1OperationalStatus Status { get; set; }

    [Required(ErrorMessage = "Visitor name is required.")]
    [StringLength(
        200,
        ErrorMessage =
            "Visitor name cannot exceed 200 characters.")]
    [Display(Name = "Visitor Name")]
    public string VisitorName { get; set; } =
        string.Empty;

    [Required(ErrorMessage = "Visit date is required.")]
    [Display(Name = "Visit Date")]
    public DateTime? VisitDate { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }
}