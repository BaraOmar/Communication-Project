using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationProject.Models;

public class CustomerConnection
{
    [Key]
    public Guid Id { get; set; } =
        Guid.NewGuid();

    [Required]
    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } =
        null!;

    [Required]
    public Guid CommunicationPathId { get; set; }

    [ForeignKey(nameof(CommunicationPathId))]
    public CommunicationPath CommunicationPath { get; set; } =
        null!;

    // Identifies all E1s that belong to this customer's connection.
    public Guid ConnectionGroupId { get; set; } =
        Guid.NewGuid();

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } =
        true;

    public DateTime CreatedAt { get; set; } =
        DateTime.Now;
    [NotMapped]
    public string ConnectionGroupDisplay =>
    ConnectionGroupId.ToString();
    public ICollection<CustomerConnectionSegment> Segments { get; set; } =
    new List<CustomerConnectionSegment>();
}