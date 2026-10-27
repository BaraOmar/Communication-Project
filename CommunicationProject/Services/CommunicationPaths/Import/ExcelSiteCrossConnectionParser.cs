using CommunicationProject.ViewModels;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunicationProject.Models;

namespace CommunicationProject.Services.CommunicationPaths.Import;

public static class ExcelSiteCrossConnectionParser
{
    private static readonly Regex StmPattern = Pattern(
        @"^STM(?:\s+(?<stm>[1-9][0-9]*))?\s*" +
        @"\(\s*(?:(?<shelf>[1-9][0-9]*)\s*\.\s*)?" +
        @"(?<card>[1-9][0-9]*)\s*\.\s*" +
        @"(?<port>[1-9][0-9]*)\s*\)\s*" +
        @"E1\s*\(\s*(?<e1>[1-3]\.[1-7]\.[1-3])\s*\)$");

    private static readonly Regex TellabPattern = Pattern(
        @"^VC4\s+(?<vc4>[0-9]+)\s+" +
        @"E1\s*\(\s*(?<e1>[1-3]\.[1-7]\.[1-3])\s*\)$");

    private static readonly Regex XmpPattern = Pattern(
        @"^SC4\s+(?<card>II|I)\s+TU12\s+(?<tu12>[1-9][0-9]*)$");

    private static Regex Pattern(string expression) => new(
        expression,
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
        text = text.Trim();
        string? muxName = null;

        const string separator = " — ";
        int separatorIndex = text.LastIndexOf(
            separator, StringComparison.Ordinal);

        InvalidDataException Invalid() => new(
            $"Data row {rowNumber}, {side} side: expected " +
            "STM 1 (shelf.card.port) E1 (1.1.1), " +
            "STM 1 (card.port) E1 (1.1.1), " +
            "VC4 5 E1 (1.1.1), or SC4 I TU12 15. " +
            "An optional MUX name can precede ' — '.");

        if (separatorIndex >= 0)
        {
            muxName = text[..separatorIndex].Trim();
            text = text[(separatorIndex + separator.Length)..].Trim();

            if (muxName.Length is < 1 or > 100)
                throw Invalid();
        }

        var match = StmPattern.Match(text);

        if (match.Success)
        {
            int shelf = 0;

            if (match.Groups["shelf"].Success &&
                (!int.TryParse(match.Groups["shelf"].Value, out shelf) ||
                 shelf is < 1 or > 1000))
            {
                throw Invalid();
            }

            if (!int.TryParse(match.Groups["card"].Value, out int card) ||
                !int.TryParse(match.Groups["port"].Value, out int port) ||
                card is < 1 or > 1000 ||
                port is < 1 or > 1000)
            {
                throw Invalid();
            }

            return new ExcelStmE1Reference(
                match.Groups["stm"].Success
                    ? match.Groups["stm"].Value
                    : null,
                shelf,
                card,
                port,
                match.Groups["e1"].Value,
                muxName);
        }

        match = TellabPattern.Match(text);

        if (match.Success)
        {
            if (!int.TryParse(match.Groups["vc4"].Value, out int vc4) ||
                vc4 is < 0 or > 15)
            {
                throw Invalid();
            }

            return new ExcelStmE1Reference(
                null,
                0,
                1,
                vc4 + 1,
                match.Groups["e1"].Value,
                muxName,
                MuxModel.Tellab);
        }

        match = XmpPattern.Match(text);

        if (match.Success)
        {
            if (!int.TryParse(match.Groups["tu12"].Value, out int tu12) ||
                tu12 is < 1 or > 63)
            {
                throw Invalid();
            }

            int index = tu12 - 1;

            string e1 = FormattableString.Invariant(
                $"{index / 21 + 1}.{index % 21 / 3 + 1}.{index % 3 + 1}");

            return new ExcelStmE1Reference(
                null,
                0,
                match.Groups["card"].Value.Equals(
                    "I", StringComparison.OrdinalIgnoreCase) ? 1 : 2,
                1,
                e1,
                muxName,
                MuxModel.Xmp);
        }

        throw Invalid();
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