using CommunicationProject.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateMuxCardViewModel
{
    public Guid MuxId { get; set; }

    [ValidateNever]
    public string MuxName { get; set; } = string.Empty;

    [ValidateNever]
    public MuxModel Model { get; set; }

    [EnumDataType(typeof(CardCategory))]
    [Display(Name = "Card Category")]
    public CardCategory Category { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Shelf Number")]
    public int ShelfNumber { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Card Number")]
    public int SlotNumber { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Number of Ports")]
    public int PortCount { get; set; } = 1;
}