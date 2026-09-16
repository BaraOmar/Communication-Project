using CommunicationProject.ViewModels;
using System.Globalization;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal static class ExcelCommunicationLinkParser
{
    public static List<ExcelCommunicationLinkRow> Parse(
        ExcelFileUploadItemViewModel file)
    {
        ArgumentNullException.ThrowIfNull(file);

        int siteFromIndex =
            FindColumn(file.Headers, "Site from");

        int siteToIndex =
            FindColumn(file.Headers, "Site to");

        int stmCapacityIndex =
            FindColumn(file.Headers, "STM-Capacity");

        int linkTypeIndex =
            FindColumn(file.Headers, "Link Type");


        var result =
            new List<ExcelCommunicationLinkRow>();


        foreach (var row in file.Rows)
        {
            string siteFrom =
                NormalizeSiteId(
                    GetValue(row, siteFromIndex));

            string siteTo =
                NormalizeSiteId(
                    GetValue(row, siteToIndex));

            string capacityText =
                GetValue(row, stmCapacityIndex);

            string linkType =
                GetValue(row, linkTypeIndex)
                    .Trim();


            // Ignore completely empty rows.
            if (string.IsNullOrWhiteSpace(siteFrom) &&
                string.IsNullOrWhiteSpace(siteTo) &&
                string.IsNullOrWhiteSpace(capacityText) &&
                string.IsNullOrWhiteSpace(linkType))
            {
                continue;
            }


            if (string.IsNullOrWhiteSpace(siteFrom))
            {
                throw new InvalidDataException(
                    "Site from is required.");
            }

            if (string.IsNullOrWhiteSpace(siteTo))
            {
                throw new InvalidDataException(
                    "Site to is required.");
            }

            if (!int.TryParse(
                    capacityText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int stmCapacity) ||
                stmCapacity <= 0)
            {
                throw new InvalidDataException(
                    $"Invalid STM capacity '{capacityText}'.");
            }

            if (string.IsNullOrWhiteSpace(linkType))
            {
                throw new InvalidDataException(
                    "Link Type is required.");
            }


            result.Add(
                new ExcelCommunicationLinkRow(
                    siteFrom,
                    siteTo,
                    stmCapacity,
                    linkType));
        }


        return result;
    }


    private static int FindColumn(
        IReadOnlyList<string> headers,
        string columnName)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            if (string.Equals(
                    headers[i].Trim(),
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidDataException(
            $"Required column '{columnName}' was not found.");
    }


    private static string GetValue(
        IReadOnlyList<string> row,
        int index)
    {
        if (index < 0 ||
            index >= row.Count)
        {
            return string.Empty;
        }

        return row[index]?.Trim()
            ?? string.Empty;
    }


    private static string NormalizeSiteId(
        string value)
    {
        value = value.Trim();

        /*
         * Keep the same site-ID behavior already used
         * by the existing Access importer:
         *
         * 5   -> 005
         * 36  -> 036
         * 125 -> 125
         */
        if (int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int numericSiteId))
        {
            return numericSiteId.ToString(
                "000",
                CultureInfo.InvariantCulture);
        }

        return value;
    }
}