using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class Mux
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Site")]
    public string SiteId { get; set; } =
        string.Empty;

    [ForeignKey(nameof(SiteId))]
    public Site Site { get; set; } =
        null!;

    [Required]
    [Display(Name = "Communication Link")]
    public Guid CommunicationLinkId { get; set; }

    [ForeignKey(nameof(CommunicationLinkId))]
    public CommunicationLink CommunicationLink { get; set; } =
        null!;
    public ICollection<MuxCard> Cards { get; set; } =
    new List<MuxCard>();
}