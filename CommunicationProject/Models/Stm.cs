using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

/*
 * رقم STM يجب أن يكون فريدًا داخل سجل الرابط الاتجاهي.
 *
 * الرابط الأول:
 * Amman -> Zarqa / STM 1
 *
 * الرابط العكسي:
 * Zarqa -> Amman / STM 1
 *
 * مسموح لأن LinkId مختلف.
 */
[Index(
    nameof(LinkId),
    nameof(Number),
    IsUnique = true)]

/*
 * كل STM يمكن أن يكون الطرف المقابل لـ STM واحد فقط.
 */
[Index(
    nameof(ConnectedStmId),
    IsUnique = true)]
public class Stm : IValidatableObject
{
    [Key]
    [Display(Name = "STM ID")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "STM number is required.")]
    [StringLength(
        20,
        ErrorMessage = "STM number cannot exceed 20 characters.")]
    [RegularExpression(
        @"^[1-9][0-9]*$",
        ErrorMessage = "STM number must be a positive whole number.")]
    [Display(Name = "STM Number")]
    public string Number { get; set; } = string.Empty;

    /*
     * يحدد سجل الرابط الاتجاهي الذي ينتمي إليه STM.
     *
     * موقع STM هو دائمًا Link.SiteFromId.
     */
    [Required]
    [Display(Name = "Communication Link")]
    public Guid LinkId { get; set; }

    [ForeignKey(nameof(LinkId))]
    public CommunicationLink Link { get; set; } = null!;

    /*
     * STM المقابل الموجود داخل سجل الرابط العكسي.
     *
     * Amman -> Zarqa / STM 1
     *                 ↕
     * Zarqa -> Amman / STM 1
     */
    [Display(Name = "Connected STM")]
    public Guid? ConnectedStmId { get; set; }

    [ForeignKey(nameof(ConnectedStmId))]
    public Stm? ConnectedStm { get; set; }

    /*
     * كل STM يحتوي على 63 قناة E1.
     */
    public ICollection<E1> E1Channels { get; set; } = new List<E1>();

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Id == Guid.Empty)
        {
            yield return new ValidationResult(
                "STM ID is required.",
                new[] { nameof(Id) });
        }

        if (LinkId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Communication link is required.",
                new[] { nameof(LinkId) });
        }

        if (ConnectedStmId.HasValue &&
            ConnectedStmId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "The connected STM is invalid.",
                new[] { nameof(ConnectedStmId) });
        }

        if (ConnectedStmId.HasValue &&
            ConnectedStmId.Value == Id)
        {
            yield return new ValidationResult(
                "An STM cannot connect to itself.",
                new[] { nameof(ConnectedStmId) });
        }
    }
}