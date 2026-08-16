using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

[Index(nameof(StmId), nameof(E1Number), IsUnique = true)]
[Index(nameof(ConnectedE1Id), IsUnique = true)]
public class E1 : IValidatableObject
{
    [Key]
    [Display(Name = "E1 ID")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "E1 number is required.")]
    [StringLength(
        5,
        MinimumLength = 5,
        ErrorMessage = "E1 number must use the format 1.1.1.")]
    [RegularExpression(
        @"^[1-3]\.[1-7]\.[1-3]$",
        ErrorMessage = "E1 number must be between 1.1.1 and 3.7.3.")]
    [Display(Name = "E1 Number")]
    public string E1Number { get; set; } = string.Empty;

    [Display(Name = "STM")]
    public Guid StmId { get; set; }

    [ForeignKey(nameof(StmId))]
    public Stm Stm { get; set; } = null!;

    [Display(Name = "Connected E1")]
    public Guid? ConnectedE1Id { get; set; }

    [Display(Name = "Join E1")]
    public Guid? JoinE1Id { get; set; }
    [Display(Name = "Path ID")]
    public Guid? PathId { get; set; }

    public E1CrossConnectionState CrossConnectionState { get; set; }
    = E1CrossConnectionState.Available;

    [Range(1, int.MaxValue)]
    [Display(Name = "Path Order")]
    public int? PathOrder { get; set; }

    [ForeignKey(nameof(ConnectedE1Id))]
    public E1? ConnectedE1 { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StmId == Guid.Empty)
        {
            yield return new ValidationResult(
                "STM is required.",
                new[] { nameof(StmId) });
        }

        if (ConnectedE1Id.HasValue &&
            ConnectedE1Id.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "The selected connected E1 is invalid.",
                new[] { nameof(ConnectedE1Id) });
        }

        if (ConnectedE1Id.HasValue &&
            ConnectedE1Id.Value == Id)
        {
            yield return new ValidationResult(
                "An E1 channel cannot connect to itself.",
                new[] { nameof(ConnectedE1Id) });
        }
    }
}