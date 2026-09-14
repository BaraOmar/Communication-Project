using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class AssignE1ToMuxPortViewModel
{
    [Required]
    public Guid MuxPortId { get; set; }

    public Guid MuxCardId { get; set; }
    public int PortNumber { get; set; }

    public string MuxName { get; set; } =
        string.Empty;

    public string CardTypeName { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Physical E1")]
    public Guid? E1Id { get; set; }

    public List<SelectListItem> E1Options { get; set; } =
        new();
}