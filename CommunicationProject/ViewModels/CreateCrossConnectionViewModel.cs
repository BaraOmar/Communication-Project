using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateCrossConnectionViewModel
{
    /* =====================================================
       Cross-connection site
       ===================================================== */

    [Required(
        ErrorMessage =
            "Select the cross-connection site.")]
    [Display(
        Name = "Cross Connection Site")]
    public string SiteId { get; set; } =
        string.Empty;


    /* =====================================================
   Incoming side
   ===================================================== */

    [Required(
        ErrorMessage =
            "Select the incoming resource.")]
    [Display(Name = "Previous Site")]
    public string IncomingResourceKey { get; set; }
        = string.Empty;


    /*
     * Derived by the server from IncomingResourceKey.
     */
    [ValidateNever]
    public string PreviousSiteId { get; set; }
        = string.Empty;

    [ValidateNever]
    public Guid? IncomingLinkId { get; set; }

    [ValidateNever]
    public Guid? IncomingStmId { get; set; }


    [Required(
        ErrorMessage =
            "Select the incoming E1.")]
    [Display(Name = "Incoming E1")]
    public Guid? IncomingE1Id { get; set; }


    /* =====================================================
       Outgoing side
       ===================================================== */

    [Required(
        ErrorMessage =
            "Select the outgoing resource.")]
    [Display(Name = "Next Site")]
    public string OutgoingResourceKey { get; set; }
        = string.Empty;


    /*
     * Derived by the server from OutgoingResourceKey.
     */
    [ValidateNever]
    public string NextSiteId { get; set; }
        = string.Empty;

    [ValidateNever]
    public Guid? OutgoingLinkId { get; set; }

    [ValidateNever]
    public Guid? OutgoingStmId { get; set; }


    [Required(
        ErrorMessage =
            "Select the outgoing E1.")]
    [Display(Name = "Outgoing E1")]
    public Guid? OutgoingE1Id { get; set; }

    /* =====================================================
       Customer
       ===================================================== */

    [Required]
    [Display(
        Name = "Customer")]
    public string CustomerName { get; set; } =
        string.Empty;


    /* =====================================================
       Options
       ===================================================== */

    [ValidateNever]
    public List<SelectListItem> SiteOptions
    {
        get;
        set;
    } = new();
}