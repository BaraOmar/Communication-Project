using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

/*
 * يسمح بوجود سجلين يحملان الاسم نفسه:
 *
 * Primary = true
 * Primary = false
 *
 * لكنه يمنع إنشاء زوج آخر بالاسم نفسه.
 */
[Index(
    nameof(Name),
    nameof(IsPrimary),
    IsUnique = true)]

/*
 * يمنع تكرار الاتجاه نفسه لنفس نوع الرابط:
 *
 * Amman -> Zarqa
 * Amman -> Zarqa
 */
[Index(
    nameof(LinkTypeId),
    nameof(SiteFromId),
    nameof(SiteToId),
    IsUnique = true)]

/*
 * كل سجل رابط يمكن أن يكون مرتبطًا بسجل عكسي واحد فقط.
 */
[Index(
    nameof(ConnectedLinkId),
    IsUnique = true)]
public class CommunicationLink : IValidatableObject
{
    [Key]
    [Display(Name = "Communication Link ID")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Link name is required.")]
    [StringLength(
        200,
        ErrorMessage = "Link name cannot exceed 200 characters.")]
    [Display(Name = "Link Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Link Type")]
    public Guid LinkTypeId { get; set; }

    [ForeignKey(nameof(LinkTypeId))]
    public LinkType LinkType { get; set; } = null!;

    /*
     * في كل سجل اتجاهي، SiteFrom هو الموقع الذي تنتمي
     * إليه وحدات STM الموجودة داخل هذا السجل.
     */
    [Required(ErrorMessage = "Source site is required.")]
    [Display(Name = "Site From")]
    [StringLength(100)]

    public string SiteFromId { get; set; } = string.Empty;

    [ForeignKey(nameof(SiteFromId))]
    public Site SiteFrom { get; set; } = null!;

    [Required(ErrorMessage = "Destination site is required.")]
    [StringLength(100)]
    [Display(Name = "Site To")]
    public string SiteToId { get; set; } = string.Empty;

    [ForeignKey(nameof(SiteToId))]
    public Site SiteTo { get; set; } = null!;

    /*
     * يربط السجل بالسجل العكسي.
     *
     * Amman -> Zarqa
     * مرتبط مع
     * Zarqa -> Amman
     */
    [Display(Name = "Connected Link")]
    public Guid? ConnectedLinkId { get; set; }

    [ForeignKey(nameof(ConnectedLinkId))]
    public CommunicationLink? ConnectedLink { get; set; }

    [Display(Name = "Capacity")]
    [StringLength(
    100,
    ErrorMessage = "Capacity cannot exceed 100 characters.")]
    public string? Capacity { get; set; }

    /*
     * السجل الأساسي فقط يظهر في قوائم المستخدم.
     *
     * true  = السجل الظاهر
     * false = السجل العكسي الداخلي
     */
    [Display(Name = "Primary Link Record")]
    public bool IsPrimary { get; set; }

    /*
     * كل سجل اتجاهي يحتوي على STMs الخاصة بموقع SiteFrom.
     *
     * مثال:
     * Amman -> Zarqa يحتوي على STMs الخاصة بعمان.
     */
    public ICollection<Stm> Stms { get; set; } = new List<Stm>();

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Id == Guid.Empty)
        {
            yield return new ValidationResult(
                "Communication link ID is required.",
                new[] { nameof(Id) });
        }

        if (LinkTypeId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Link type is required.",
                new[] { nameof(LinkTypeId) });
        }

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

        if (ConnectedLinkId.HasValue &&
            ConnectedLinkId.Value == Guid.Empty)
        {
            yield return new ValidationResult(
                "The connected link is invalid.",
                new[] { nameof(ConnectedLinkId) });
        }

        if (ConnectedLinkId.HasValue &&
            ConnectedLinkId.Value == Id)
        {
            yield return new ValidationResult(
                "A communication link cannot connect to itself.",
                new[] { nameof(ConnectedLinkId) });
        }
    }
}