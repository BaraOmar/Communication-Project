using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class Mux
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Site")]
    public string SiteId { get; set; } = string.Empty;

    [ForeignKey(nameof(SiteId))]
    public Site Site { get; set; } = null!;

    [Display(Name = "MUX Type")]
    public Guid? MuxTypeId { get; set; }

    [ForeignKey(nameof(MuxTypeId))]
    public MuxType? MuxType { get; set; }
    [Range(
    1,
    100,
    ErrorMessage = "Shelf count must be at least 1.")]
    [Display(Name = "Number of Shelves")]
    public int? ShelfCount { get; set; }


    [Range(
        1,
        1000,
        ErrorMessage = "Card slot count must be at least 1.")]
    [Display(Name = "Card Slots")]
    public int? CardSlotCount { get; set; }
    public ICollection<MuxCard> Cards { get; set; }
        = new List<MuxCard>();
}