using System.Text.Json.Nodes;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// The books the seeded rows come from. Each importer passes an entry through <see cref="Of"/>,
/// which notes the book and the edition the row is in; Books.json is written from what was noted.
/// </summary>
internal static class SourceBooks
{
    // The titles 5etools gives the books (data/books.json and data/adventures.json)
    private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
    {
        ["PHB"] = "Player's Handbook (2014)",
        ["XPHB"] = "Player's Handbook (2024)",
        ["DMG"] = "Dungeon Master's Guide (2014)",
        ["XGE"] = "Xanathar's Guide to Everything",
        ["TCE"] = "Tasha's Cauldron of Everything",
        ["SCAG"] = "Sword Coast Adventurer's Guide",
        ["EGW"] = "Explorer's Guide to Wildemount",
        ["FTD"] = "Fizban's Treasury of Dragons",
        ["MTF"] = "Mordenkainen's Tome of Foes",
        ["VRGR"] = "Van Richten's Guide to Ravenloft",
        ["BGG"] = "Bigby Presents: Glory of the Giants",
        ["BMT"] = "The Book of Many Things",
        ["SatO"] = "Sigil and the Outlands",
        ["AAG"] = "Spelljammer: Astral Adventurer's Guide",
        ["SCC"] = "Strixhaven: A Curriculum of Chaos",
        ["GGR"] = "Guildmasters' Guide to Ravnica",
        ["ERLW"] = "Eberron: Rising from the Last War",
        ["EFA"] = "Eberron: Forge of the Artificer",
        ["FRHoF"] = "Forgotten Realms: Heroes of Faerûn",
        ["ABH"] = "Astarion's Book of Hungers",
        ["LFL"] = "Lorwyn: First Light",
        ["RHW"] = "Ravenloft: The Horrors Within",
        ["AU"] = "Arcana Unleashed",
        ["MOT"] = "Mythic Odysseys of Theros",
        ["DSotDQ"] = "Dragonlance: Shadow of the Dragon Queen",
        ["AI"] = "Acquisitions Incorporated",
        ["AitFR-AVT"] = "Adventures in the Forgotten Realms: A Verdant Tomb",
        ["GoS"] = "Ghosts of Saltmarsh",
        ["BGDIA"] = "Baldur's Gate: Descent into Avernus",
        ["IDRotF"] = "Icewind Dale: Rime of the Frostmaiden",
        ["WBtW"] = "The Wild Beyond the Witchlight",
        ["ToA"] = "Tomb of Annihilation",
        ["LLK"] = "Lost Laboratory of Kwalish",
        ["PSA"] = "Plane Shift: Amonkhet",
        ["PSK"] = "Plane Shift: Kaladesh",
        ["PSI"] = "Plane Shift: Innistrad",
        ["PSX"] = "Plane Shift: Ixalan",
        ["UATheMysticClass"] = "Unearthed Arcana: The Mystic Class",
    };

    private static readonly HashSet<string> PlayersHandbooks = ["PHB", "XPHB"];

    private static readonly SortedDictionary<string, string> EditionByCode = new(StringComparer.Ordinal);

    /// <summary>The book's title, or its code for a book this table does not know.</summary>
    public static string Title(string code) => Titles.GetValueOrDefault(code, code);

    /// <summary>
    /// The code of the book the entry comes from, noted with the edition of the row made from
    /// it. A book with no title, or with rows in both editions, stops the run.
    /// </summary>
    public static string Of(JsonObject entry, string edition)
    {
        var code = entry.Str("source") ?? throw new InvalidDataException($"{entry.Str("name")} has no source");

        if (!Titles.ContainsKey(code))
        {
            throw new InvalidDataException($"{entry.Str("name")} is from {code}, which has no title in SourceBooks");
        }

        if (EditionByCode.TryGetValue(code, out var noted) && noted != edition)
        {
            throw new InvalidDataException($"{code} has rows in {noted} and {edition}; a book is one edition");
        }

        EditionByCode[code] = edition;
        return code;
    }

    /// <summary>Every book a row was made from, for Books.json.</summary>
    public static List<BookSeedRow> Used() =>
        EditionByCode
            .Select(book => new BookSeedRow
            {
                Code = book.Key,
                Name = Titles[book.Key],
                Edition = book.Value,
                IsPlayersHandbook = PlayersHandbooks.Contains(book.Key),
            })
            .ToList();
}
