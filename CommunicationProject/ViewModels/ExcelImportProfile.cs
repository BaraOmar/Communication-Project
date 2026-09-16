namespace CommunicationProject.ViewModels;

public class ExcelImportProfile
{
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    // The row that contains the actual column names.
    public int HeaderRow { get; set; } = 1;

    // Columns expected for this Excel type.
    public List<string> RequiredColumns { get; set; } = [];

    public Dictionary<string, List<string>> ColumnAliases { get; set; } = [];
}