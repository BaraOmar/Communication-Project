using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunicationProject.Strategies.Capacity;

public static class StmCapacityParser
{
    private static readonly Regex StmRegex =
        new(
            @"STM\b",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

    private static readonly Regex StmCountRegex =
        new(
            @"(?<count>\d+)\s*(?:X\s*)?STM\b",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

    public static bool CanHandle(string? capacity)
    {
        return !string.IsNullOrWhiteSpace(capacity) &&
               StmRegex.IsMatch(capacity);
    }

    public static int? ExtractCount(string? capacity)
    {
        if (string.IsNullOrWhiteSpace(capacity))
        {
            return null;
        }

        Match match =
            StmCountRegex.Match(capacity);

        if (match.Success &&
            int.TryParse(
                match.Groups["count"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int stmCount))
        {
            return stmCount;
        }

        /*
         * Capacity such as STM-1 does not have a number
         * before STM, so it represents one STM.
         */
        return StmRegex.IsMatch(capacity)
            ? 1
            : null;
    }
}