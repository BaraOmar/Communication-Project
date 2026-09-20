using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class AssignStmToMuxPortViewModel
{
    public Guid MuxPortId { get; set; }

    public Guid MuxCardId { get; set; }

    public string MuxName { get; set; } = string.Empty;

    public string SiteName { get; set; } = string.Empty;

    public int PortNumber { get; set; }

    public int SlotNumber { get; set; }



    [Required]
    [Display(Name = "STM")]
    public Guid? StmId { get; set; }

    public List<SelectListItem> StmOptions { get; set; }
        = new();
}