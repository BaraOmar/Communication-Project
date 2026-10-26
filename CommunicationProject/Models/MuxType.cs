using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class MuxType
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "MUX Type Name")]
    public string Name { get; set; } = string.Empty;



    public ICollection<Mux> Muxes { get; set; } =
        new List<Mux>();

    [Range(0, 100)]
    [Display(Name = "Number of LTU Cards")]
    public int LtuCardCount { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Ports per LTU Card")]
    public int LtuPortsPerCard { get; set; }

    [Range(0, 100)]
    [Display(Name = "Number of STM Cards")]
    public int StmCardCount { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Ports per STM Card")]
    public int StmPortsPerCard { get; set; }




}