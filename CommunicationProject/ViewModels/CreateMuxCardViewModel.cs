using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateMuxCardViewModel
{
    [Required]
    public Guid MuxId { get; set; }

    public string MuxName { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Card Type")]
    public Guid? CardTypeId { get; set; }
    [Range(
    1,
    1000,
    ErrorMessage = "Shelf number must be at least 1.")]
    [Display(Name = "Shelf Number")]
    public int? ShelfNumber { get; set; }
    [Required]
    [Range(
        1,
        1000,
        ErrorMessage = "Slot number must be at least 1.")]
    [Display(Name = "Slot Number")]
    public int SlotNumber { get; set; }

    public bool HasShelves { get; set; }

    public int? ShelfCount { get; set; }

    public int? CardSlotCount { get; set; } 
    public List<SelectListItem> CardTypeOptions { get; set; } =
        new();

    public List<int> OccupiedSlotNumbers { get; set; }
    = new();

    public Dictionary<int, List<int>> OccupiedSlotsByShelf { get; set; }
        = new();
}