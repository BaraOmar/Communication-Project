using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class CustomerConnectionSegment
{
    [Key]
    public Guid Id { get; set; } =
        Guid.NewGuid();

    [Required]
    public Guid CustomerConnectionId { get; set; }

    [ForeignKey(nameof(CustomerConnectionId))]
    public CustomerConnection CustomerConnection { get; set; } =
        null!;

    [Required]
    public Guid CommunicationPathSegmentId { get; set; }

    [ForeignKey(nameof(CommunicationPathSegmentId))]
    public CommunicationPathSegment CommunicationPathSegment { get; set; } =
        null!;

    [Required]
    public Guid E1Id { get; set; }

    [ForeignKey(nameof(E1Id))]
    public E1 E1 { get; set; } =
        null!;
}