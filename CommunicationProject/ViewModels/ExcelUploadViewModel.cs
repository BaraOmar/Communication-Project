namespace CommunicationProject.ViewModels;

public class ExcelUploadViewModel
{
    public List<ExcelImportProfile> Profiles { get; set; } = [];

    public List<ExcelFileUploadItemViewModel> Files { get; set; } = [];

    public bool HasData =>
        Files.Any(x => x.HasData);
}