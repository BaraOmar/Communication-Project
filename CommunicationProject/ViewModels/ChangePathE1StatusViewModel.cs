using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.ViewModels;

public sealed class ChangePathE1StatusViewModel : IValidatableObject
{
    public Guid CustomerConnectionId { get; set; }

    public List<Guid> E1Ids { get; set; } = new();

    [Required(ErrorMessage = "Select a status.")]
    public E1OperationalStatus? Status { get; set; }

    [Required(ErrorMessage = "Visitor name is required.")]
    [StringLength(200)]
    public string VisitorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Visit date is required.")]
    [DataType(DataType.Date)]
    public DateTime? VisitDate { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [ValidateNever]
    public string? ReturnUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (CustomerConnectionId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Select a customer connection.",
                new[] { nameof(CustomerConnectionId) });
        }

        if (E1Ids == null ||
            E1Ids.Count == 0 ||
            E1Ids.Any(id => id == Guid.Empty))
        {
            yield return new ValidationResult(
                "Select at least one E1 endpoint.",
                new[] { nameof(E1Ids) });
        }

        if (Status.HasValue &&
            Status.Value is not (
                E1OperationalStatus.Available or
                E1OperationalStatus.Wrong or
                E1OperationalStatus.Connected))
        {
            yield return new ValidationResult(
                "Select Available, Wrong, or Connected.",
                new[] { nameof(Status) });
        }

        if (Status == E1OperationalStatus.Connected &&
            string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult(
                "A description is required when marking an E1 Connected.",
                new[] { nameof(Description) });
        }
    }
}

public sealed class PathE1EndpointViewModel
{
    public Guid E1Id { get; set; }

    public string SiteId { get; set; } = string.Empty;

    public string LinkName { get; set; } = string.Empty;

    public string DisplayText { get; set; } = string.Empty;

    public E1OperationalStatus Status { get; set; }
}