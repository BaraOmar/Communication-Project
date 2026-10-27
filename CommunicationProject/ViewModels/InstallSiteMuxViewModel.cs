using CommunicationProject.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class InstallSiteMuxViewModel
{
    [Required]
    public string SiteId { get; set; } = string.Empty;

    [ValidateNever]
    public string SiteName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Installed MUX Name")]
    public string MuxName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a MUX type.")]
    [Display(Name = "MUX Type")]
    public Guid? MuxTypeId { get; set; }

    [ValidateNever]
    public MuxModel Model { get; set; }

    [Range(29, 31)]
    [Display(Name = "First Physical STM")]
    public int? FirstPhysicalStmNumber { get; set; }

    [Range(29, 31)]
    [Display(Name = "Second Physical STM")]
    public int? SecondPhysicalStmNumber { get; set; }

    public List<MuxTypeCardInputViewModel> Cards { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> MuxTypeOptions { get; set; } = [];

    [ValidateNever]
    public List<SiteSdhStmPortAssignmentViewModel> StmAssignments
    { get; set; } = [];
}

public class SiteSdhStmPortAssignmentViewModel
{
    public Guid StmId { get; set; }

    public string StmNumber { get; set; } = string.Empty;

    public string LinkName { get; set; } = string.Empty;

    public string RemoteSiteId { get; set; } = string.Empty;

    // Section:Shelf:Card:Port
    public string? PortKey { get; set; }
}