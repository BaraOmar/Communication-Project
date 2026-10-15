using CommunicationProject.ViewModels;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunicationProject.Services.CommunicationPaths.Import;

public static class ExcelSiteCrossConnectionParser
{
    private static readonly Regex EndpointPattern = new(
        @"^\s*STM\s+(?<stm>[1-9][0-9]*)\s*" +
        @"\(\s*(?<shelf>[1-9][0-9]*)\s*\.\s*" +
        @"(?<slot>[1-9][0-9]*)\s*\.\s*" +
        @"(?<port>[1-9][0-9]*)\s*\)\s*" +
        @"E1\s*\(\s*(?<e1>[1-3]\.[1-7]\.[1-3])\s*\)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static List<ExcelSiteCrossConnectionRow> Parse(
        ExcelFileUploadItemViewModel file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Headers.Count != 2)
        {
            throw new InvalidDataException(
                "Use exactly two columns: Current Site and Cross Connection.");
        }

        int siteColumn = FindColumn(
            file.Headers, "Current Site");

        int connectionColumn = FindColumn(
            file.Headers, "Cross Connection");

        var result = new List<ExcelSiteCrossConnectionRow>();

        for (int i = 0; i < file.Rows.Count; i++)
        {
            var row = file.Rows[i];

            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            if (row.Count != 2)
            {
                throw new InvalidDataException(
                    $"Data row {i + 1}: expected two cells.");
            }

            string site = row[siteColumn]?.Trim() ?? "";
            string text = row[connectionColumn]?.Trim() ?? "";

            if (site.Length == 0 || site.Length > 100)
            {
                throw new InvalidDataException(
                    $"Data row {i + 1}: invalid Current Site.");
            }

            if (text.Length == 0 || text.Length > 1000)
            {
                throw new InvalidDataException(
                    $"Data row {i + 1}: invalid Cross Connection.");
            }

            var sides = text.Split('*');

            if (sides.Length != 2)
            {
                throw new InvalidDataException(
                    $"Data row {i + 1}: use exactly one * " +
                    "between the incoming and outgoing resources.");
            }

            result.Add(new ExcelSiteCrossConnectionRow(
                i + 1,
                site,
                ParseEndpoint(sides[0], i + 1, "left"),
                ParseEndpoint(sides[1], i + 1, "right")));
        }

        if (result.Count == 0)
        {
            throw new InvalidDataException(
                "The file contains no cross-connections.");
        }

        return result;
    }

    private static ExcelStmE1Reference ParseEndpoint(
        string text,
        int rowNumber,
        string side)
    {
        var match = EndpointPattern.Match(text);

        if (!match.Success ||
            !int.TryParse(
                match.Groups["stm"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int stm) ||
            !int.TryParse(
                match.Groups["shelf"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int shelf) ||
            !int.TryParse(
                match.Groups["slot"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int slot) ||
            !int.TryParse(
                match.Groups["port"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int port) ||
            shelf != 1 ||
            slot > 1000 ||
            port > 1000)
        {
            throw new InvalidDataException(
                $"Data row {rowNumber}, {side} side: expected " +
                "STM 1 (1.1.1) E1 (1.1.1). Shelf must be 1.");
        }

        return new ExcelStmE1Reference(
            stm.ToString(CultureInfo.InvariantCulture),
            shelf,
            slot,
            port,
            match.Groups["e1"].Value);
    }

    private static int FindColumn(
        IReadOnlyList<string> headers,
        string name)
    {
        var matches = headers
            .Select((header, index) => new { header, index })
            .Where(item => string.Equals(
                item.header?.Trim(),
                name,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count != 1)
        {
            throw new InvalidDataException(
                $"Column '{name}' must appear exactly once.");
        }

        return matches[0].index;
    }
}