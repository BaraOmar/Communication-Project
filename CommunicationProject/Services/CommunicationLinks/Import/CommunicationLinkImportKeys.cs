namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal static class CommunicationLinkImportKeys
{
    public static string GetPairKey(
        string siteAId,
        string siteBId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            siteAId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            siteBId);

        return string.Compare(
                   siteAId,
                   siteBId,
                   StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{siteAId}|{siteBId}"
            : $"{siteBId}|{siteAId}";
    }

    public static string GetDirectionalRouteKey(
        Guid linkTypeId,
        string siteFromId,
        string siteToId)
    {
        if (linkTypeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Link type identifier cannot be empty.",
                nameof(linkTypeId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            siteFromId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            siteToId);

        return
            $"{linkTypeId:N}|" +
            $"{siteFromId.Trim().ToUpperInvariant()}|" +
            $"{siteToId.Trim().ToUpperInvariant()}";
    }
}