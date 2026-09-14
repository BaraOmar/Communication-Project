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

    [Required]
    [Range(
        1,
        1000,
        ErrorMessage = "Slot number must be at least 1.")]
    [Display(Name = "Slot Number")]
    public int SlotNumber { get; set; }

    public List<SelectListItem> CardTypeOptions { get; set; } =
        new();
}