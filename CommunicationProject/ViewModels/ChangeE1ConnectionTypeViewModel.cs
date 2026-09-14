using CommunicationProject.Models;
using System.ComponentModel.DataAnnotations;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.ViewModels;

public class ChangeE1ConnectionTypeViewModel
{
    [Required]
    public Guid E1Id { get; set; }

    public string E1Number { get; set; } =
        string.Empty;

    [Display(Name = "Current Type")]
    public E1ConnectionType CurrentType { get; set; }

    [Required]
    [Display(Name = "Connection Type")]
    public E1ConnectionType ConnectionType { get; set; }
}