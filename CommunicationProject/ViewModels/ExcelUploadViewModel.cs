using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CommunicationProject.ViewModels;

public class ExcelUploadViewModel
{
    [Display(Name = "Excel File")]
    public IFormFile? ExcelFile { get; set; }

    public string? FileName { get; set; }

    public string? WorksheetName { get; set; }

    public List<string> Headers { get; set; } = [];

    public List<List<string>> Rows { get; set; } = [];

    public bool HasData =>
        Headers.Count > 0;
}