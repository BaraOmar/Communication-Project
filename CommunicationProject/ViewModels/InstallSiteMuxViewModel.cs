using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class InstallSiteMuxViewModel
{
    [Required]
    public string SiteId { get; set; } =
        string.Empty;


    [ValidateNever]
    public string SiteName { get; set; } =
        string.Empty;


    [Required]
    [StringLength(100)]
    [Display(Name = "Installed MUX Name")]
    public string MuxName { get; set; } =
        string.Empty;


    [Required(
        ErrorMessage = "Select a MUX type.")]
    [Display(Name = "MUX Type")]
    public Guid? MuxTypeId { get; set; }


    [ValidateNever]
    public List<SelectListItem> MuxTypeOptions
    { get; set; } = new();


    [ValidateNever]
    public List<SiteSdhStmPortAssignmentViewModel>
        StmAssignments
    { get; set; } = new();
}


public class SiteSdhStmPortAssignmentViewModel
{
    public Guid StmId { get; set; }


    [ValidateNever]
    public string StmNumber { get; set; } =
        string.Empty;


    [ValidateNever]
    public string LinkName { get; set; } =
        string.Empty;


    [ValidateNever]
    public string RemoteSiteId { get; set; } =
        string.Empty;


    /*
     * Format:
     *
     * slotNumber:portNumber
     *
     * Example:
     *
     * 3:1
     */
    [Display(Name = "MUX STM Port")]
    public string? PortKey { get; set; }
}