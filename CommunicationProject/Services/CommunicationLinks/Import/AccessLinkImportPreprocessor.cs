namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal static class AccessLinkImportPreprocessor
{
    public static AccessLinkPreparationResult Prepare(
        IEnumerable<AccessLinkRow> sourceRows)
    {
        ArgumentNullException.ThrowIfNull(
            sourceRows);

        List<AccessLinkRow> rows =
            sourceRows.ToList();

        IReadOnlyList<string> conflictingNames =
            FindConflictingNames(rows);

        IReadOnlyList<AccessLinkRow> distinctRows =
            RemoveDuplicateRows(rows);

        IReadOnlyList<string> requiredSiteIds =
            GetRequiredSiteIds(distinctRows);

        return new AccessLinkPreparationResult(
            distinctRows,
            requiredSiteIds,
            conflictingNames);
    }

    private static IReadOnlyList<string>
        FindConflictingNames(
            IEnumerable<AccessLinkRow> rows)
    {
        return rows
            .GroupBy(
                row => row.Name,
                StringComparer.OrdinalIgnoreCase)
            .Where(group =>
                group
                    .Select(row =>
                        CommunicationLinkImportKeys
                            .GetPairKey(
                                row.SiteAId,
                                row.SiteBId))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name)
            .ToList();
    }

    private static IReadOnlyList<AccessLinkRow>
        RemoveDuplicateRows(
            IEnumerable<AccessLinkRow> rows)
    {
        return rows
            .GroupBy(
                row => BuildLogicalLinkKey(row),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static IReadOnlyList<string>
        GetRequiredSiteIds(
            IEnumerable<AccessLinkRow> rows)
    {
        return rows
            .SelectMany(row =>
                new[]
                {
                    row.SiteAId,
                    row.SiteBId
                })
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(siteId => siteId)
            .ToList();
    }

    private static string BuildLogicalLinkKey(
        AccessLinkRow row)
    {
        string pairKey =
            CommunicationLinkImportKeys
                .GetPairKey(
                    row.SiteAId,
                    row.SiteBId);

        return $"{row.Name}|{pairKey}";
    }
}