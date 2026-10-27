using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models;

public class MuxType
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "MUX Type Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "MUX Model")]
    public MuxModel Model { get; set; }

    public ICollection<MuxTypeCard> DefaultCards { get; set; }
        = new List<MuxTypeCard>();

    public ICollection<Mux> Muxes { get; set; }
        = new List<Mux>();
}