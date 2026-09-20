using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class MuxCard
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "MUX")]
    public Guid MuxId { get; set; }

    [ForeignKey(nameof(MuxId))]
    public Mux Mux { get; set; } =
        null!;

    [Required]
    [Display(Name = "Card Category")]
    public CardCategory Category { get; set; }


    [Range(
        1,
        1000,
        ErrorMessage = "Slot number must be at least 1.")]
    [Display(Name = "Slot Number")]
    public int SlotNumber { get; set; }
    public ICollection<MuxPort> Ports { get; set; } =
    new List<MuxPort>();
}