using CommunicationProject.Models;

namespace CommunicationProject.Services.Muxes;

public static class MuxDisplay
{
    public static string CardPosition(MuxCard card)
    {
        var parts = new List<string>();

        if (card.Section != MuxSection.Standard)
            parts.Add(card.Section.ToString());

        if (card.ShelfNumber > 0)
            parts.Add($"Shelf {card.ShelfNumber}");

        parts.Add($"Card {card.SlotNumber}");

        return string.Join(" · ", parts);
    }

    public static string StmLabel(
        MuxModel model,
        int shelfNumber,
        int cardNumber,
        int portNumber,
        string? linkStmNumber = null)
    {
        if (model == MuxModel.Tellab)
            return $"VC4 {portNumber - 1}";

        if (model == MuxModel.Xmp)
        {
            return cardNumber switch
            {
                1 => "SC4 I",
                2 => "SC4 II",
                _ => throw new ArgumentOutOfRangeException(nameof(cardNumber))
            };
        }

        string position = MuxStructureRules.UsesShelves(model)
            ? $"{shelfNumber}.{cardNumber}.{portNumber}"
            : $"{cardNumber}.{portNumber}";

        string name = string.IsNullOrWhiteSpace(linkStmNumber)
            ? "STM"
            : $"STM {linkStmNumber}";

        return $"{name} ({position})";
    }

    public static string LtuLabel(
        MuxModel model,
        int shelfNumber,
        int cardNumber,
        int portNumber,
        int? physicalStmNumber = null)
    {
        string shelf = MuxStructureRules.UsesShelves(model)
            ? $"Shelf {shelfNumber} "
            : string.Empty;

        string physical =
            model == MuxModel.Tellab && physicalStmNumber.HasValue
                ? $"VC4 {physicalStmNumber.Value - 1} "
                : string.Empty;

        return $"{physical}{shelf}LTU {cardNumber} Port {portNumber}";
    }

    public static string E1Label(MuxModel model, string number)
    {
        if (model != MuxModel.Xmp)
            return $"E1 ({number})";

        string[] parts = number.Split('.');

        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out int first) ||
            !int.TryParse(parts[1], out int second) ||
            !int.TryParse(parts[2], out int third) ||
            first is < 1 or > 3 ||
            second is < 1 or > 7 ||
            third is < 1 or > 3)
        {
            throw new ArgumentException(
                "An SDH E1 number must be between 1.1.1 and 3.7.3.",
                nameof(number));
        }

        int tu12 = (first - 1) * 21 + (second - 1) * 3 + third;

        return $"TU12 {tu12}";
    }

    public static string PathEndpoint(
    MuxModel model,
    int shelfNumber,
    int cardNumber,
    int portNumber,
    string e1Number)
    {
        if (model == MuxModel.Tellab)
            return $"VC4 {portNumber - 1} ({e1Number})";

        if (model == MuxModel.Xmp)
        {
            return StmLabel(
                model,
                shelfNumber,
                cardNumber,
                portNumber)
                + " " + E1Label(model, e1Number);
        }

        string position = MuxStructureRules.UsesShelves(model)
            ? $"{shelfNumber}.{cardNumber}.{portNumber}"
            : $"{cardNumber}.{portNumber}";

        return $"({position}) ({e1Number})";
    }
}