using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public sealed class ConfigureStmsViewModel
{
    [Required]
    public Guid LinkId { get; set; }

    [Range(
        1,
        100,
        ErrorMessage = "The number of STMs must be between 1 and 100.")]
    [Display(Name = "STMs for Site From")]
    public int SiteFromStmCount { get; set; } = 1;

    [Range(
        1,
        100,
        ErrorMessage = "The number of STMs must be between 1 and 100.")]
    [Display(Name = "STMs for Site To")]
    public int SiteToStmCount { get; set; } = 1;

    public string LinkName { get; set; } = string.Empty;

    public string SiteFromName { get; set; } = string.Empty;

    public string SiteToName { get; set; } = string.Empty;
}