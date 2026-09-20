using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace CommunicationProject.Models;

[Index(nameof(ConnectedE1Id), IsUnique = true)]
[Index(nameof(ConnectionGroupId))]
public class E1 : IValidatableObject
{
    [Key]
    [Display(Name = "E1 ID")]
    public Guid Id { get; set; }


    [Required(ErrorMessage = "E1 number is required.")]
    [StringLength(
        20,
        ErrorMessage = "E1 number cannot exceed 20 characters.")]
    [Display(Name = "E1 Number")]
    public string E1Number { get; set; } = string.Empty;


    /*
     * Every E1 belongs to a directional link.
     *
     * SDH: Link + STM
     * PDH: Link directly, without STM
     */
    [Required]
    [Display(Name = "Communication Link")]
    public Guid LinkId { get; set; }

    [ForeignKey(nameof(LinkId))]
    public CommunicationLink Link { get; set; } = null!;


    [Display(Name = "STM")]
    public Guid? StmId { get; set; }

    [ForeignKey(nameof(StmId))]
    public Stm? Stm { get; set; }


    [Display(Name = "Connected E1")]
    public Guid? ConnectedE1Id { get; set; }

    [ForeignKey(nameof(ConnectedE1Id))]
    public E1? ConnectedE1 { get; set; }


    [Display(Name = "Join E1")]
    public Guid? JoinE1Id { get; set; }


    [Display(Name = "Connection Group")]
    public Guid? ConnectionGroupId { get; set; }


    [Display(Name = "Status")]
    public E1OperationalStatus Status { get; set; }
        = E1OperationalStatus.Available;


    [StringLength(
        200,
        ErrorMessage = "Visitor name cannot exceed 200 characters.")]
    [Display(Name = "Visitor")]
    public string? VisitorName { get; set; }


    [Display(Name = "Visit Date")]
    public DateTime? VisitDate { get; set; }


    public E1CrossConnectionState CrossConnectionState { get; set; }
        = E1CrossConnectionState.Available;


    [StringLength(
        1000,
        ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }


    [Display(Name = "Connection Type")]
    public E1ConnectionType ConnectionType { get; set; }
        = E1ConnectionType.Unassigned;


    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (LinkId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Communication link is required.",
                new[] { nameof(LinkId) });
        }


        if (StmId.HasValue &&
            StmId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "The selected STM is invalid.",
                new[] { nameof(StmId) });
        }


        if (StmId.HasValue)
        {
            bool validSdhNumber =
                Regex.IsMatch(
                    E1Number,
                    @"^[1-3]\.[1-7]\.[1-3]$");

            if (!validSdhNumber)
            {
                yield return new ValidationResult(
                    "An SDH E1 number must be between 1.1.1 and 3.7.3.",
                    new[] { nameof(E1Number) });
            }
        }
        else
        {
            bool validPdhNumber =
                Regex.IsMatch(
                    E1Number,
                    @"^[1-9][0-9]*$");

            if (!validPdhNumber)
            {
                yield return new ValidationResult(
                    "A PDH E1 number must be a positive whole number.",
                    new[] { nameof(E1Number) });
            }
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


        if (ConnectionGroupId.HasValue &&
            ConnectionGroupId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "The connection group is invalid.",
                new[] { nameof(ConnectionGroupId) });
        }
    }


    public enum E1OperationalStatus
    {
        Available,
        Connected,
        Wrong,
        Damaged
    }


    public enum E1ConnectionType
    {
        Unassigned,
        Physical,
        Logical
    }
}