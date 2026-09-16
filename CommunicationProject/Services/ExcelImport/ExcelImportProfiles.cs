using CommunicationProject.ViewModels;

namespace CommunicationProject.Services.ExcelImport;

public static class ExcelImportProfiles
{
    public static List<ExcelImportProfile> All =>
    [
        new()
        {
            Key = "generic",
            Name = "General Excel Sheet",
            Description = "Upload and preview a standard Excel file.",
            HeaderRow = 1,
            RequiredColumns  = []
        },

new()
{
    Key = "cross-connections",
    Name = "Cross Connections",
    Description = "Upload cross-connection and MUX data.",
    HeaderRow = 2,

    RequiredColumns =
    [
        "Cross-Connections",
        "MUX/M.W TYPE",
        "Site ID To",
        "Site ID From",
        "#"
    ],

    ColumnAliases = new()
    {
        ["Cross-Connections"] =
        [
            "Cross Connections",
            "Cross Connection"
        ],

        ["MUX/M.W TYPE"] =
        [
            "MUX Type",
            "MUX/MW TYPE",
            "MUX M.W TYPE"
        ],

        ["Site ID To"] =
        [
            "Site To",
            "To Site ID"
        ],

        ["Site ID From"] =
        [
            "Site From",
            "From Site ID"
        ],

        ["#"] =
        [
            "No",
            "No.",
            "Number"
        ]
    }
},

new()
{
    Key = "communication-links",
    Name = "Communication Links",
    Description =
        "Import communication links, STM capacity, and link types.",

    HeaderRow = 1,

    RequiredColumns =
    [
        "Site from",
        "Site to",
        "STM-Capacity",
        "Link Type"
    ]
}
    ];
}