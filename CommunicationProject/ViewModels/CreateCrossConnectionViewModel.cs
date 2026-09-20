using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateCrossConnectionViewModel
{
    [Required(ErrorMessage = "Select the cross-connection site.")]
    [Display(Name = "Cross Connection Site")]
    public string SiteId { get; set; } = string.Empty;


    [Required(ErrorMessage = "Select the previous site.")]
    [Display(Name = "Previous Site")]
    public string PreviousSiteId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select the incoming link.")]
    [Display(Name = "Incoming Link")]
    public Guid? IncomingLinkId { get; set; }

    [Display(Name = "Incoming STM")]
    public Guid? IncomingStmId { get; set; }

    [Required(ErrorMessage = "Select the incoming E1.")]
    [Display(Name = "Incoming E1")]
    public Guid? IncomingE1Id { get; set; }


    [Required(ErrorMessage = "Select the next site.")]
    [Display(Name = "Next Site")]
    public string NextSiteId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select the outgoing link.")]
    [Display(Name = "Outgoing Link")]
    public Guid? OutgoingLinkId { get; set; }

    [Display(Name = "Outgoing STM")]
    public Guid? OutgoingStmId { get; set; }

    [Required(ErrorMessage = "Select the outgoing E1.")]
    [Display(Name = "Outgoing E1")]
    public Guid? OutgoingE1Id { get; set; }


    [Required]
    [Display(Name = "Customer")]
    public string CustomerName { get; set; } =
        string.Empty;


    [ValidateNever]
    public List<SelectListItem> SiteOptions
    { get; set; } = new();
}