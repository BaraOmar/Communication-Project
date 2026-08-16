using System.Data.OleDb;
using System.Globalization;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal static class AccessLinkFileReader
{
    public static List<AccessLinkRow> Read(
        string accessFilePath)
    {
        if (string.IsNullOrWhiteSpace(
                accessFilePath))
        {
            throw new ArgumentException(
                "The Access file path is required.",
                nameof(accessFilePath));
        }

        if (!File.Exists(accessFilePath))
        {
            throw new FileNotFoundException(
                "The Access database file does not exist.",
                accessFilePath);
        }

        var rows =
            new List<AccessLinkRow>();

        var connectionStringBuilder =
            new OleDbConnectionStringBuilder
            {
                Provider =
                    "Microsoft.ACE.OLEDB.12.0",

                DataSource =
                    accessFilePath
            };

        using var connection =
            new OleDbConnection(
                connectionStringBuilder
                    .ConnectionString);

        using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                [Link Name (A-B)] AS LinkName,
                [Site A] AS SiteA,
                [Site B] AS SiteB,
                [CAPACITY] AS Capacity
            FROM [LINKS]
            """;

        connection.Open();

        using OleDbDataReader reader =
            command.ExecuteReader()
            ?? throw new InvalidDataException(
                "The Access LINKS table could not be read.");

        while (reader.Read())
        {
            string name =
                Convert.ToString(
                    reader["LinkName"],
                    CultureInfo.InvariantCulture)
                ?.Trim()
                ?? string.Empty;

            string siteAId =
                NormalizeSiteId(
                    reader["SiteA"]);

            string siteBId =
                NormalizeSiteId(
                    reader["SiteB"]);

            string capacity =
                Convert.ToString(
                    reader["Capacity"],
                    CultureInfo.InvariantCulture)
                ?.Trim()
                ?? string.Empty;

            /*
             * Ignore incomplete Access rows.
             */
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(siteAId) ||
                string.IsNullOrWhiteSpace(siteBId))
            {
                continue;
            }

            rows.Add(
                new AccessLinkRow(
                    name,
                    siteAId,
                    siteBId,
                    capacity));
        }

        return rows;
    }

    private static string NormalizeSiteId(
        object? value)
    {
        if (value == null ||
            value == DBNull.Value)
        {
            return string.Empty;
        }

        string text =
            Convert.ToString(
                value,
                CultureInfo.InvariantCulture)
            ?.Trim()
            ?? string.Empty;

        /*
         * Preserve the previous behavior:
         *
         * 5   -> 005
         * 36  -> 036
         * 125 -> 125
         *
         * Text and Arabic identifiers remain unchanged.
         */
        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int numericSiteId))
        {
            return numericSiteId.ToString(
                "000",
                CultureInfo.InvariantCulture);
        }

        return text;
    }
}