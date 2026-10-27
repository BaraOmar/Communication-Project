using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class MuxTypeCard
{
    public Guid Id { get; set; }

    public Guid MuxTypeId { get; set; }

    [ForeignKey(nameof(MuxTypeId))]
    public MuxType MuxType { get; set; } = null!;

    public CardCategory Category { get; set; }

    public MuxSection Section { get; set; }
        = MuxSection.Standard;

    // 0 means this model does not use shelves.
    [Range(0, 1000)]
    public int ShelfNumber { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Card Number")]
    public int SlotNumber { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Number of Ports")]
    public int PortCount { get; set; }

    // Required cards cannot be omitted during installation.
    public bool IsRequired { get; set; } = true;
}