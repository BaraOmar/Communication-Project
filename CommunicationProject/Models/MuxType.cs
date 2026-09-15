using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.Models
{
    public class MuxType
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool HasShelves { get; set; }

        public ICollection<Mux> Muxes { get; set; }
            = new List<Mux>();
    }
}