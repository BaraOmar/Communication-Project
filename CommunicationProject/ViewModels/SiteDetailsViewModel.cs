using CommunicationProject.Models;

namespace CommunicationProject.ViewModels;

public class SiteDetailsViewModel
{
    public Site Site { get; set; } = null!;

    public List<CommunicationLink> Links { get; set; }
        = new();

    public List<Mux> Muxes { get; set; }
        = new();

    public bool HasSdhLinks { get; set; }
}