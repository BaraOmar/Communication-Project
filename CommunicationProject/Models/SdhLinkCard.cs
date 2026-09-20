using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class SdhLinkCard
{
    [Key]
    public Guid Id { get; set; }


    [Required]
    [Display(Name = "Communication Link")]
    public Guid LinkId { get; set; }

    [ForeignKey(nameof(LinkId))]
    public CommunicationLink Link { get; set; } = null!;


    [Range(
        1,
        20,
        ErrorMessage = "Card number must be between 1 and 20.")]
    [Display(Name = "Card Number")]
    public int Number { get; set; }


    public ICollection<Stm> Stms { get; set; }
        = new List<Stm>();
}