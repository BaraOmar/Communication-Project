using CommunicationProject.Models;

namespace CommunicationProject.ViewModels;

public class SiteDetailsViewModel
{
    public Site Site { get; set; } = null!;

    public List<CommunicationLink> Links { get; set; } = new();

    public List<Mux> Muxes { get; set; } = new();

    public bool HasSdhLinks { get; set; }

    public List<E1> E1s { get; set; } = new();

    public string? E1Status { get; set; }

    public int E1PageNumber { get; set; } = 1;

    public int E1PageSize { get; set; } = 25;

    public int E1TotalItems { get; set; }

    public int E1TotalPages =>
        (int)Math.Ceiling(
            E1TotalItems / (double)E1PageSize);
}