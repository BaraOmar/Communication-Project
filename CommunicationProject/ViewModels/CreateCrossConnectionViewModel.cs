using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateCrossConnectionViewModel
{
    [Required(ErrorMessage = "Select the cross-connection site.")]
    [Display(Name = "Cross Connection Site")]
    public string SiteId { get; set; } = string.Empty;

    /*
     * Previous side:
     * the site from which the connection arrives.
     */
    [Required(ErrorMessage = "Select the previous site.")]
    [Display(Name = "Previous Site")]
    public string PreviousSiteId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select the incoming STM.")]
    [Display(Name = "Incoming STM")]
    public Guid? IncomingStmId { get; set; }

    [Required(ErrorMessage = "Select the incoming E1.")]
    [Display(Name = "Incoming E1")]
    public Guid? IncomingE1Id { get; set; }

    /*
     * Next side:
     * the site to which the connection leaves.
     */
    [Required(ErrorMessage = "Select the next site.")]
    [Display(Name = "Next Site")]
    public string NextSiteId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select the outgoing STM.")]
    [Display(Name = "Outgoing STM")]
    public Guid? OutgoingStmId { get; set; }

    [Required(ErrorMessage = "Select the outgoing E1.")]
    [Display(Name = "Outgoing E1")]
    public Guid? OutgoingE1Id { get; set; }


    [Required]
    [Display(Name = "Customer")]
    public string CustomerName { get; set; } =
    string.Empty;

    /*
     * Only the main Site dropdown is loaded when the page opens.
     * Other dropdowns are loaded dynamically through AJAX.
     */
    [ValidateNever]
    public List<SelectListItem> SiteOptions { get; set; } = new();
}