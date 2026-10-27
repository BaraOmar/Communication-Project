using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class AssignE1ToMuxPortViewModel
{
    public Guid MuxPortId { get; set; }

    [ValidateNever]
    public Guid MuxCardId { get; set; }

    [ValidateNever]
    public int PortNumber { get; set; }

    [ValidateNever]
    public string MuxName { get; set; } = string.Empty;

    [ValidateNever]
    public string CardCategoryName { get; set; } = string.Empty;

    [ValidateNever]
    public string Position { get; set; } = string.Empty;

    [ValidateNever]
    public bool IsTellab { get; set; }

    [Required]
    [Display(Name = "Physical E1")]
    public Guid? E1Id { get; set; }

    [Range(29, 31)]
    [Display(Name = "Physical VC4")]
    public int? PhysicalStmNumber { get; set; }

    [ValidateNever]
    public List<SelectListItem> E1Options { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> PhysicalStmOptions { get; set; } = [];
}