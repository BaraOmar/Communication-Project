using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class ImportCommunicationLinksViewModel
{
    [Required(ErrorMessage = "Select an Access database file.")]
    [Display(Name = "Access Database")]
    public IFormFile? AccessFile { get; set; }

    [Required(ErrorMessage = "Select a link type.")]
    [Display(Name = "Link Type")]
    public Guid? LinkTypeId { get; set; }

    public List<SelectListItem> LinkTypeOptions { get; set; } = [];
}