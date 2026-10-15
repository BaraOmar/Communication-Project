using CommunicationProject.ViewModels;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunicationProject.Services.CommunicationPaths.Import;

public static class ExcelCommunicationPathParser
{
    public static List<ExcelCommunicationPathRow> Parse(
        ExcelFileUploadItemViewModel file)
    {
        ArgumentNullException.ThrowIfNull(file);

        string[] requiredColumns =
        [
            "CircuitKey",
            "CustomerName",
            "Order",
            "SiteFrom",
            "SiteTo",
            "LinkName",
            "STMNumber",
            "E1Number"
        ];

        var indexes = new Dictionary<string, int>();

        foreach (string column in requiredColumns)
        {
            var matches = file.Headers
                .Select((header, index) => new { header, index })
                .Where(item => string.Equals(
                    item.header?.Trim(),
                    column,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count != 1)
            {
                throw new InvalidDataException(
                    $"The column '{column}' must appear exactly once.");
            }

            indexes[column] = matches[0].index;
        }
        var muxColumns = file.Headers
    .Select((header, index) => new { header, index })
    .Where(item => string.Equals(
        item.header?.Trim(),
        "MuxName",
        StringComparison.OrdinalIgnoreCase))
    .ToList();

        if (muxColumns.Count > 1)
        {
            throw new InvalidDataException(
                "MuxName must not appear more than once.");
        }

        if (muxColumns.Count == 1)
        {
            indexes["MuxName"] = muxColumns[0].index;
        }
        var result = new List<ExcelCommunicationPathRow>();

        for (int i = 0; i < file.Rows.Count; i++)
        {
            var row = file.Rows[i];

            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            string Get(string column)
            {
                if (!indexes.TryGetValue(column, out int index))
                {
                    return "";
                }

                return index < row.Count
                    ? row[index]?.Trim() ?? ""
                    : "";
            }

            string prefix = $"Data row {i + 1}: ";

            foreach (string column in requiredColumns)
            {
                // Empty STMNumber means PDH.
                if (column != "STMNumber" &&
                    Get(column).Length == 0)
                {
                    throw new InvalidDataException(
                        prefix + $"{column} is required.");
                }
            }

            if (!int.TryParse(
                    Get("Order"),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int order) ||
                order < 1)
            {
                throw new InvalidDataException(
                    prefix + "Order must be a positive whole number.");
            }

            string? stmNumber = null;
            string stmText = Get("STMNumber");

            if (stmText.Length > 0)
            {
                var parts = stmText.Split('.');

                if (parts.Length != 3 ||
                    !int.TryParse(
                        parts[0].Trim(),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int shelf) ||
                    !int.TryParse(
                        parts[1].Trim(),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int slot) ||
                    !int.TryParse(
                        parts[2].Trim(),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int port) ||
                    shelf < 1 ||
                    slot < 1 || slot > 1000 ||
                    port < 1 || port > 1000)
                {
                    throw new InvalidDataException(
                        prefix +
                        "STMNumber must be shelf.card.port, for example 1.1.2.");
                }

                if (shelf != 1)
                {
                    throw new InvalidDataException(
                        prefix +
                        "Only shelf 1 is supported by the current MUX model.");
                }

                stmNumber = string.Join(
                    ".",
                    shelf.ToString(CultureInfo.InvariantCulture),
                    slot.ToString(CultureInfo.InvariantCulture),
                    port.ToString(CultureInfo.InvariantCulture));
            }


            string e1Number = Get("E1Number");

            string pattern = stmNumber == null
                ? @"^[1-9][0-9]*$"
                : @"^[1-3]\.[1-7]\.[1-3]$";

            if (e1Number.Length > 20 ||
                !Regex.IsMatch(e1Number, pattern))
            {
                throw new InvalidDataException(
                    prefix +
                    "Invalid E1Number for the selected SDH/PDH format.");
            }

            if (Get("CustomerName").Length > 200)
            {
                throw new InvalidDataException(
                    prefix +
                    "CustomerName cannot exceed 200 characters.");
            }

            if (string.Equals(
                    Get("SiteFrom"),
                    Get("SiteTo"),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    prefix + "SiteFrom and SiteTo must be different.");
            }

            result.Add(new ExcelCommunicationPathRow(
                i + 1,
                Get("CircuitKey"),
                Get("CustomerName"),
                order,
                Get("SiteFrom"),
                Get("SiteTo"),
                Get("LinkName"),
                stmNumber,
                e1Number,
                Get("MuxName").Length == 0
                    ? null
                    : Get("MuxName")));
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException(
                "The file contains no path data.");
        }

        foreach (var circuit in result.GroupBy(
                     row => row.CircuitKey,
                     StringComparer.OrdinalIgnoreCase))
        {
            var rows = circuit
                .OrderBy(row => row.Order)
                .ToList();

            int customerCount = rows
                .Select(row => row.CustomerName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            if (customerCount != 1)
            {
                throw new InvalidDataException(
                    $"Circuit '{circuit.Key}' must have one customer.");
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Order != i + 1)
                {
                    throw new InvalidDataException(
                        $"Circuit '{circuit.Key}' requires unique orders " +
                        "starting at 1 without gaps.");
                }

                if (i > 0 &&
                    !string.Equals(
                        rows[i - 1].SiteToId,
                        rows[i].SiteFromId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Circuit '{circuit.Key}' is disconnected " +
                        $"between orders {i} and {i + 1}.");
                }
            }
        }

        return result;
    }
}