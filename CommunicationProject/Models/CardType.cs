using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models;

public class CardType
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } =
        string.Empty;

    [Required]
    [Display(Name = "Category")]
    public CardCategory Category { get; set; }

    [Range(
        1,
        1000,
        ErrorMessage = "Port count must be at least 1.")]
    [Display(Name = "Port Count")]
    public int PortCount { get; set; }

    public ICollection<MuxCard> Cards { get; set; } =
    new List<MuxCard>();
}

public enum CardCategory
{
    STM,
    E1,
    Power
}