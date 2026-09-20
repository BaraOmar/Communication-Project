using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class CreateMuxTypeViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "MUX Type Name")]
    public string Name { get; set; } = string.Empty;


    [Range(0, 100)]
    [Display(Name = "Number of Power Cards")]
    public int PowerCardCount { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Ports per Power Card")]
    public int PowerPortsPerCard { get; set; }


    [Range(0, 100)]
    [Display(Name = "Number of STM Cards")]
    public int StmCardCount { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Ports per STM Card")]
    public int StmPortsPerCard { get; set; }


    [Range(0, 100)]
    [Display(Name = "Number of E1 Cards")]
    public int E1CardCount { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Ports per E1 Card")]
    public int E1PortsPerCard { get; set; }
}