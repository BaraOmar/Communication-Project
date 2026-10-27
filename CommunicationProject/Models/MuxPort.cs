using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class MuxPort
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "MUX Card")]
    public Guid MuxCardId { get; set; }

    [ForeignKey(nameof(MuxCardId))]
    public MuxCard MuxCard { get; set; } =
        null!;

    [Range(
        1,
        1000,
        ErrorMessage = "Port number must be at least 1.")]
    [Display(Name = "Port Number")]
    public int PortNumber { get; set; }

    [Display(Name = "E1")]
    public Guid? E1Id { get; set; }

    [ForeignKey(nameof(E1Id))]
    public E1? E1 { get; set; }

    [Display(Name = "Status")]
    public MuxPortStatus Status { get; set; }
        = MuxPortStatus.Available;

    [Display(Name = "STM")]
    public Guid? StmId { get; set; }

    [ForeignKey(nameof(StmId))]
    public Stm? Stm { get; set; }

    // Tellab LTU assignment only.
    // Stores the actual selected physical STM: 29, 30, or 31.
    // Null when no physical STM is assigned.
    [Range(29, 31)]
    [Display(Name = "Physical STM")]
    public int? PhysicalStmNumber { get; set; }

    [NotMapped]
    public string Location =>
        MuxCard == null
            ? string.Empty
            : $"{MuxCard.SlotNumber}.{PortNumber}";

    [NotMapped]
    public string PhysicalPosition =>
        MuxCard == null
            ? string.Empty
            : $"Card {MuxCard.SlotNumber} / Port {PortNumber}";

}

public enum MuxPortStatus
{
    Available,
    Connected,
    Wrong,
    Damaged
}

