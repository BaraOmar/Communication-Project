using Microsoft.AspNetCore.Http;

namespace CommunicationProject.ViewModels;

public class ExcelFileUploadItemViewModel
{
    public string ProfileKey { get; set; } = "";

    public string ProfileName { get; set; } = "";

    public IFormFile? ExcelFile { get; set; }

    public string? FileName { get; set; }

    public string? WorksheetName { get; set; }

    public List<string> Headers { get; set; } = [];

    public List<List<string>> Rows { get; set; } = [];

    public bool HasData =>
        Headers.Count > 0;
}