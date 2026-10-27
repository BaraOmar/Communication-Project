using CommunicationProject.Models;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateMuxTypeViewModel
{
    [Required(ErrorMessage = "Select a MUX model.")]
    [EnumDataType(typeof(MuxModel))]
    [Display(Name = "MUX Model")]
    public MuxModel? Model { get; set; }

    public List<MuxTypeCardInputViewModel> Cards { get; set; } = [];
}

public class MuxTypeCardInputViewModel
{
    [EnumDataType(typeof(CardCategory))]
    public CardCategory Category { get; set; }

    [EnumDataType(typeof(MuxSection))]
    public MuxSection Section { get; set; }

    [Range(0, 1000)]
    public int ShelfNumber { get; set; }

    [Range(1, 1000)]
    public int SlotNumber { get; set; } = 1;

    [Range(1, 1000)]
    public int PortCount { get; set; } = 1;

    public bool IsRequired { get; set; } = true;
}