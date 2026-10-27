using CommunicationProject.Models;

namespace CommunicationProject.Services.Muxes;

public static class MuxStructureRules
{
    public static bool IsSupported(MuxModel model) =>
        model != MuxModel.Undefined &&
        Enum.IsDefined(model);

    public static bool UsesShelves(MuxModel model) =>
        model is MuxModel.Oms1600 or MuxModel.Oms1200;

    public static bool HasFixedStructure(MuxModel model) =>
        model is MuxModel.Tellab or MuxModel.Xmp;

    public static string GetName(MuxModel model) => model switch
    {
        MuxModel.Oms1600 => "OMS 1600",
        MuxModel.Oms1200 => "OMS 1200",
        MuxModel.Oms800 => "OMS 800",
        MuxModel.Spo1410 => "SPO 1410",
        MuxModel.Tellab => "Tellab",
        MuxModel.Xmp => "XMP",
        _ => throw new ArgumentOutOfRangeException(nameof(model))
    };

    public static List<MuxTypeCard> GetDefaultCards(MuxModel model)
    {
        if (model == MuxModel.Tellab)
        {
            return
            [
                Card(CardCategory.STM, MuxSection.Main, 1, 16),
                Card(CardCategory.LTU, MuxSection.Sub, 1, 32),
                Card(CardCategory.LTU, MuxSection.Sub, 2, 32)
            ];
        }

        if (model == MuxModel.Xmp)
        {
            return
            [
                Card(CardCategory.STM, MuxSection.Standard, 1, 1),
                Card(CardCategory.STM, MuxSection.Standard, 2, 1)
            ];
        }

        return [];
    }

    private static MuxTypeCard Card(
        CardCategory category,
        MuxSection section,
        int number,
        int ports) => new()
        {
            Category = category,
            Section = section,
            SlotNumber = number,
            PortCount = ports,
            IsRequired = true
        };

    public static List<string> Validate(
        MuxModel model,
        IReadOnlyCollection<MuxTypeCard> cards)
    {
        var errors = new List<string>();

        if (!IsSupported(model))
        {
            errors.Add("Select a supported MUX model.");
            return errors;
        }

        if (cards.Count is < 1 or > 100)
            errors.Add("Configure between 1 and 100 cards.");

        if (!cards.Any(c => c.Category == CardCategory.STM))
            errors.Add("At least one STM card is required.");

        foreach (var card in cards)
        {
            if (!Enum.IsDefined(card.Category) ||
                !Enum.IsDefined(card.Section))
                errors.Add("A card has an invalid category or section.");

            if (card.SlotNumber is < 1 or > 1000 ||
                card.PortCount is < 1 or > 1000)
                errors.Add("Card numbers and port counts must be between 1 and 1000.");

            if (UsesShelves(model))
            {
                if (card.ShelfNumber is < 1 or > 1000)
                    errors.Add("Every card requires a shelf number between 1 and 1000.");
            }
            else if (card.ShelfNumber != 0)
            {
                errors.Add("This model does not use shelf numbers.");
            }

            if (model != MuxModel.Tellab &&
                card.Section != MuxSection.Standard)
                errors.Add("Only Tellab uses Main and Sub sections.");
        }

        if (cards.GroupBy(c => new
        {
            c.Section,
            c.ShelfNumber,
            c.SlotNumber
        }).Any(group => group.Count() > 1))
        {
            errors.Add("Two cards cannot occupy the same position.");
        }

        if (HasFixedStructure(model))
        {
            var expected = GetDefaultCards(model);

            bool matches = cards.Count == expected.Count &&
                expected.All(required => cards.Any(actual =>
                    actual.Category == required.Category &&
                    actual.Section == required.Section &&
                    actual.ShelfNumber == required.ShelfNumber &&
                    actual.SlotNumber == required.SlotNumber &&
                    actual.PortCount == required.PortCount &&
                    actual.IsRequired));

            if (!matches)
                errors.Add($"{GetName(model)} requires its predefined card structure.");
        }

        return errors.Distinct().ToList();
    }
}