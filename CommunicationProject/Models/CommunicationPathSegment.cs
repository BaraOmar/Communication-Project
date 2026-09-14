using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class CommunicationPathSegment
{
    [Key]
    public Guid Id { get; set; } =
        Guid.NewGuid();

    [Required]
    [Display(Name = "Path")]
    public Guid CommunicationPathId { get; set; }

    [ForeignKey(nameof(CommunicationPathId))]
    public CommunicationPath CommunicationPath { get; set; } =
        null!;

    [Required]
    [Display(Name = "Communication Link")]
    public Guid CommunicationLinkId { get; set; }

    [ForeignKey(nameof(CommunicationLinkId))]
    public CommunicationLink CommunicationLink { get; set; } =
        null!;

    [Range(1, int.MaxValue)]
    [Display(Name = "Segment Order")]
    public int Order { get; set; }
}