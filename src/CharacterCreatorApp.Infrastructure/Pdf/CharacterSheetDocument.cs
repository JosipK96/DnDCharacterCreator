using System.Globalization;

using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CharacterCreatorApp.Infrastructure.Pdf;

/// <summary>
/// The character sheet: page 1 laid out the way players know it (abilities, saves and skills on
/// the left, combat in the middle, proficiencies and features on the right), then features and
/// spells in full on as many pages as they take, no entry split across two. Black on white, so it
/// prints the same on any printer, and sized to fit A4 or Letter. Equipment, attacks and anything
/// else the app does not model are drawn empty, for writing in by hand.
/// </summary>
internal sealed class CharacterSheetDocument(CharacterDetailsReadModel character, SheetPaper paper) : IDocument
{
    private const float BorderWidth = 0.75f;
    private static readonly Color LabelColor = Colors.Grey.Darken3;
    private static readonly string[] AttackHeadings = ["Name", "Atk bonus", "Damage / type"];
    private static readonly string[] ProficiencyHeadings = ["Armour", "Weapons", "Tools"];

    private readonly CharacterDetailsReadModel _character = character;
    private readonly SheetPaper _paper = paper;

    // The read model carries Stats for every saved character; an empty one keeps the layout
    // drawing if it ever does not
    private CharacterStats Stats => _character.Stats ?? new CharacterStats(0, 0, 0, 0, 0, string.Empty, 0, [], [], [], [], null);

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_character.Name}, character sheet",
        Creator = "Character Creator",
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            ConfigurePage(page);
            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(6).Element(ComposeFirstPage);
        });

        container.Page(page =>
        {
            ConfigurePage(page);
            page.Header().PaddingBottom(4).BorderBottom(BorderWidth).Text(_character.Name).FontSize(11).Bold();
            page.Content().PaddingTop(6).Element(ComposeDetailPages);
        });
    }

    private void ConfigurePage(PageDescriptor page)
    {
        page.Size(_paper == SheetPaper.Letter ? PageSizes.Letter : PageSizes.A4);
        page.Margin(12, Unit.Millimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Black));

        page.Footer().PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text($"{_character.Name} · {_character.Edition} rules").FontSize(7).FontColor(LabelColor);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(7).FontColor(LabelColor));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    // --- Page 1 ---

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem(2).Border(BorderWidth).Padding(6).Column(column =>
            {
                column.Item().Text(_character.Name).FontSize(18).Bold();
                Label(column.Item(), "Character name");
            });

            row.ConstantItem(6);

            row.RelativeItem(5).Border(BorderWidth).Padding(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                HeaderField(table, ClassAndLevel(), "Class & level");
                HeaderField(table, _character.BackgroundName, "Background");
                HeaderField(table, string.Empty, "Player name");
                HeaderField(table, WithChoice(_character.RaceName, _character.SubraceName), "Species");
                HeaderField(table, _character.Size, "Size");
                HeaderField(table, $"{_character.Edition} rules", "Edition");
            });
        });
    }

    private static void HeaderField(TableDescriptor table, string value, string label)
    {
        table.Cell().Padding(2).Column(column =>
        {
            column.Item().BorderBottom(BorderWidth).MinHeight(12).Text(value).FontSize(9);
            Label(column.Item(), label);
        });
    }

    private void ComposeFirstPage(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem(10).Element(ComposeLeftColumn);
            row.ConstantItem(6);
            row.RelativeItem(9).Element(ComposeMiddleColumn);
            row.ConstantItem(6);
            row.RelativeItem(9).Element(ComposeRightColumn);
        });
    }

    private void ComposeLeftColumn(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Row(row =>
            {
                row.ConstantItem(58).Column(abilities =>
                {
                    abilities.Spacing(4);

                    foreach (var ability in Stats.Abilities)
                    {
                        abilities.Item().Element(c => AbilityBox(c, ability));
                    }
                });

                row.ConstantItem(6);

                row.RelativeItem().Column(checks =>
                {
                    checks.Spacing(6);

                    checks.Item().Row(boxes =>
                    {
                        boxes.RelativeItem().Element(c => SmallStatBox(c, string.Empty, "Inspiration"));
                        boxes.ConstantItem(4);
                        boxes.RelativeItem().Element(c => SmallStatBox(c, Signed(Stats.ProficiencyBonus), "Proficiency bonus"));
                    });

                    checks.Item().Border(BorderWidth).Padding(4).Column(saves =>
                    {
                        foreach (var save in Stats.SavingThrows)
                        {
                            saves.Item().Element(c => CheckLine(c, save.IsProficient, save.Modifier, AbilityName(save.AbilityId)));
                        }

                        Title(saves.Item().PaddingTop(3), "Saving throws");
                    });

                    checks.Item().Border(BorderWidth).Padding(4).Column(skills =>
                    {
                        foreach (var skill in Stats.Skills)
                        {
                            skills.Item().Element(c => CheckLine(c, skill.IsProficient, skill.Modifier, $"{skill.Name} ({Abbreviation(skill.AbilityId)})"));
                        }

                        Title(skills.Item().PaddingTop(3), "Skills");
                    });
                });
            });

            column.Item().Element(c => WideStatLine(c, Stats.PassivePerception.ToString(CultureInfo.InvariantCulture), "Passive Wisdom (Perception)"));
            column.Item().Extend().Element(c => WriteInBox(c, "Notes"));
        });
    }

    private static void AbilityBox(IContainer container, AbilityStat ability)
    {
        container.Border(BorderWidth).Padding(3).Column(column =>
        {
            column.Item().AlignCenter().Text(AbilityName(ability.AbilityId).ToUpperInvariant()).FontSize(6).Bold();
            column.Item().AlignCenter().Text(Signed(ability.Modifier)).FontSize(16).Bold();
            column.Item().AlignCenter().Width(26).Border(BorderWidth).AlignCenter().Text(ability.Score.ToString(CultureInfo.InvariantCulture)).FontSize(9);
        });
    }

    private void ComposeMiddleColumn(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => BigStatBox(c, Stats.ArmorClass.ToString(CultureInfo.InvariantCulture), "Armour class"));
                row.ConstantItem(4);
                row.RelativeItem().Element(c => BigStatBox(c, Signed(Stats.Initiative), "Initiative"));
                row.ConstantItem(4);
                row.RelativeItem().Element(c => BigStatBox(c, $"{Stats.Speed} ft", "Speed"));
            });

            column.Item().Text("Armour class is unarmoured: armour is not tracked yet.").FontSize(6).FontColor(LabelColor);

            column.Item().Border(BorderWidth).Padding(4).Column(hitPoints =>
            {
                hitPoints.Item().Row(max =>
                {
                    max.RelativeItem().Text("Hit point maximum").FontSize(7).FontColor(LabelColor);
                    max.ConstantItem(40).AlignRight().Text(Stats.MaxHitPoints.ToString(CultureInfo.InvariantCulture)).FontSize(11).Bold();
                });
                hitPoints.Item().Height(40);
                Title(hitPoints.Item(), "Current hit points");
            });

            column.Item().Border(BorderWidth).Padding(4).Column(temporary =>
            {
                temporary.Item().Height(26);
                Title(temporary.Item(), "Temporary hit points");
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Border(BorderWidth).Padding(4).Column(hitDice =>
                {
                    hitDice.Item().Text($"Total {Stats.HitDice}").FontSize(7);
                    hitDice.Item().Height(18);
                    Title(hitDice.Item(), "Hit dice");
                });
                row.ConstantItem(4);
                row.RelativeItem().Border(BorderWidth).Padding(4).Column(deathSaves =>
                {
                    deathSaves.Item().Element(c => TickRow(c, "Successes", 3, 38));
                    deathSaves.Item().PaddingTop(3).Element(c => TickRow(c, "Failures", 3, 38));
                    Title(deathSaves.Item().PaddingTop(3), "Death saves");
                });
            });

            // Weapons and armour come with equipment (#33): until then these are for writing in
            column.Item().Border(BorderWidth).Padding(4).Column(attacks =>
            {
                attacks.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    foreach (var heading in AttackHeadings)
                    {
                        table.Cell().Text(heading).FontSize(6).FontColor(LabelColor);
                    }

                    for (var i = 0; i < 15; i++)
                    {
                        table.Cell().PaddingRight(3).BorderBottom(BorderWidth).Height(14);
                    }
                });

                attacks.Item().Height(30);
                Title(attacks.Item(), "Attacks & spellcasting");
            });

            column.Item().Extend().Element(c => WriteInBox(c, "Equipment"));
        });
    }

    private void ComposeRightColumn(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Border(BorderWidth).Padding(4).Height(150).Column(box =>
            {
                box.Item().Height(130).ScaleToFit().Column(list =>
                {
                    ListGroup(list, "Languages", _character.Languages.Select(l => l.Name));

                    // Skills have their own list on the left
                    var others = _character.Proficiencies.Where(p => !Skill.IsSkill(p.Id)).OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

                    foreach (var heading in ProficiencyHeadings)
                    {
                        ListGroup(list, heading, others.Where(p => ProficiencyHeading(p.Type) == heading).Select(p => p.Name));
                    }
                });
                Title(box.Item(), "Other proficiencies & languages");
            });

            // The box fills what is left of the page, and a long list shrinks to fit it
            column.Item().Extend().Border(BorderWidth).Padding(4).Decoration(box =>
            {
                box.Content().Extend().ScaleToFit().Column(list =>
                {
                    foreach (var (heading, features) in FeatureGroups())
                    {
                        ListGroup(list, heading, features.Select(f => f.Name));
                    }

                    ListGroup(list, "Feats", _character.Feats.Select(f => f.Feat.Name));

                    if (_character.Features.Count > 0 || _character.Feats.Count > 0)
                    {
                        list.Item().PaddingTop(4).Text("In full on the following pages.").FontSize(6).Italic().FontColor(LabelColor);
                    }
                });
                Title(box.After().PaddingTop(4), "Features & traits");
            });
        });
    }

    // --- Pages 2 onwards ---

    private void ComposeDetailPages(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(4);

            if (_character.Features.Count > 0 || _character.Feats.Count > 0)
            {
                SectionTitle(column.Item(), "Features & traits");

                foreach (var (heading, features) in FeatureGroups())
                {
                    EntryGroup(column, heading, features.Select<FeatureReadModel, Action<IContainer>>(feature =>
                        c => Entry(c, feature.Name, $"Level {feature.Level}", feature.Description, null)));
                }

                if (_character.Feats.Count > 0)
                {
                    EntryGroup(column, "Feats", _character.Feats.OrderBy(f => f.Feat.Name, StringComparer.Ordinal).Select<CharacterFeatReadModel, Action<IContainer>>(entry =>
                    {
                        var spells = entry.Spells.Count == 0
                            ? null
                            : $"{SpellsLabel(entry.SpellcastingAbilityId)}: {string.Join(", ", entry.Spells.Select(s => s.Name))}";

                        return c => Entry(c, entry.Feat.Name, entry.Feat.Category, entry.Feat.Description, spells);
                    }));
                }
            }

            if (Stats.SpellSlots.Count > 0 || Stats.PactMagic != null || _character.Spells.Count > 0)
            {
                column.Item().PaddingTop(8).PreventPageBreak().Column(spellcasting =>
                {
                    SectionTitle(spellcasting.Item(), "Spellcasting");
                    spellcasting.Item().PaddingTop(4).Element(ComposeSpellcastingBoxes);
                });

                foreach (var level in _character.Spells.GroupBy(s => s.Level).OrderBy(g => g.Key))
                {
                    EntryGroup(column, SpellLevelHeading(level.Key), level.OrderBy(s => s.Name, StringComparer.Ordinal).Select<SpellReadModel, Action<IContainer>>(spell =>
                        c => Entry(c, spell.Name, SpellLine(spell), spell.Description, null)));
                }
            }
        });
    }

    /// <summary>
    /// The slots of each spell level, with a box to tick per slot used, and a warlock's Pact
    /// Magic apart. Spellcasting ability, save DC and attack bonus are left to write in: the
    /// sheet's numbers do not include them yet.
    /// </summary>
    private void ComposeSpellcastingBoxes(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(4);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => SmallStatBox(c, string.Empty, "Spellcasting ability"));
                row.ConstantItem(4);
                row.RelativeItem().Element(c => SmallStatBox(c, string.Empty, "Spell save DC"));
                row.ConstantItem(4);
                row.RelativeItem().Element(c => SmallStatBox(c, string.Empty, "Spell attack bonus"));
            });

            column.Item().Border(BorderWidth).Padding(4).Column(slots =>
            {
                if (Stats.SpellSlots.Count == 0 && Stats.PactMagic == null)
                {
                    slots.Item().Text("No spell slots at this level.").FontColor(LabelColor);
                }

                foreach (var slot in Stats.SpellSlots)
                {
                    slots.Item().PaddingVertical(1).Element(c => TickRow(c, $"{Ordinal(slot.SpellLevel)} level: {slot.Slots}", slot.Slots, 60));
                }

                if (Stats.PactMagic is { } pactMagic)
                {
                    slots.Item().PaddingVertical(1).Element(c => TickRow(
                        c, $"Pact Magic, {Ordinal(pactMagic.SlotLevel)} level: {pactMagic.Slots} (short rest)", pactMagic.Slots, 160));
                }

                Title(slots.Item().PaddingTop(3), "Spell slots (tick one off for each used)");
            });
        });
    }

    private static void WriteInBox(IContainer container, string title)
    {
        container.Border(BorderWidth).Padding(4).Decoration(box =>
        {
            box.Content().Extend().MinHeight(40);
            Title(box.After(), title);
        });
    }

    /// <summary>
    /// A heading and its entries, the heading kept on the page of the first entry, and no entry
    /// split across two pages unless it is longer than a page.
    /// </summary>
    private static void EntryGroup(ColumnDescriptor column, string heading, IEnumerable<Action<IContainer>> entries)
    {
        var list = entries.ToList();

        column.Item().PaddingTop(6).PreventPageBreak().Column(first =>
        {
            first.Item().PaddingBottom(2).Text(heading).FontSize(10).Bold();

            if (list.Count > 0)
            {
                first.Item().Element(list[0]);
            }
        });

        foreach (var entry in list.Skip(1))
        {
            column.Item().PreventPageBreak().Element(entry);
        }
    }

    private static void Entry(IContainer container, string name, string? subtitle, string description, string? footnote)
    {
        container.PaddingBottom(4).BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten1).PaddingBottom(3).Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span(name).FontSize(9).Bold();

                if (!string.IsNullOrEmpty(subtitle))
                {
                    text.Span($"  {subtitle}").FontSize(7).Italic().FontColor(LabelColor);
                }
            });

            if (!string.IsNullOrWhiteSpace(description))
            {
                column.Item().PaddingTop(1).Text(description).LineHeight(1.15f);
            }

            if (footnote != null)
            {
                column.Item().PaddingTop(1).Text(footnote).Italic();
            }
        });
    }

    // --- Shared pieces ---

    /// <summary>The features by where they come from: the class, the subclass, then the background.</summary>
    private List<(string Heading, List<FeatureReadModel> Features)> FeatureGroups()
    {
        var byLevel = _character.Features.OrderBy(f => f.Level).ThenBy(f => f.Name, StringComparer.Ordinal).ToList();

        return new List<(string Heading, List<FeatureReadModel> Features)>
        {
            ($"{_character.ClassName} features", byLevel.Where(f => f.ClassId != null && f.SubclassId == null).ToList()),
            ($"{_character.SubclassName ?? "Subclass"} features", byLevel.Where(f => f.SubclassId != null).ToList()),
            ($"{_character.BackgroundName} background", byLevel.Where(f => f.ClassId == null && f.SubclassId == null).ToList()),
        }
        .Where(group => group.Features.Count > 0)
        .ToList();
    }

    private static void ListGroup(ColumnDescriptor column, string heading, IEnumerable<string> names)
    {
        var list = names.ToList();

        if (list.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(3).Text(heading).FontSize(7).Bold();
        column.Item().Text(string.Join(", ", list));
    }

    private static void CheckLine(IContainer container, bool isProficient, int modifier, string name)
    {
        container.PaddingVertical(0.8f).Row(row =>
        {
            row.ConstantItem(9).AlignMiddle().Element(c => Tick(c, isProficient));
            row.ConstantItem(18).AlignRight().PaddingRight(3).BorderBottom(BorderWidth).Text(Signed(modifier));
            row.RelativeItem().PaddingLeft(3).Text(name);
        });
    }

    /// <summary>A small square, filled for a proficiency, which prints the same on any printer.</summary>
    private static void Tick(IContainer container, bool isFilled, float size = 6)
    {
        var box = container.Width(size).Height(size).Border(BorderWidth);

        if (isFilled)
        {
            box.Background(Colors.Black);
        }
    }

    private static void TickRow(IContainer container, string label, int boxes, float labelWidth)
    {
        container.Row(row =>
        {
            row.ConstantItem(labelWidth).Text(label).FontSize(7);

            for (var i = 0; i < boxes; i++)
            {
                row.ConstantItem(10).AlignMiddle().Element(c => Tick(c, false, 8));
            }

            row.RelativeItem();
        });
    }

    private static void BigStatBox(IContainer container, string value, string label)
    {
        container.Border(BorderWidth).Padding(3).Column(column =>
        {
            column.Item().AlignCenter().Text(value).FontSize(16).Bold();
            column.Item().AlignCenter().Text(label.ToUpperInvariant()).FontSize(6).Bold();
        });
    }

    private static void SmallStatBox(IContainer container, string value, string label)
    {
        container.Border(BorderWidth).Padding(3).Column(column =>
        {
            column.Item().AlignCenter().MinHeight(14).Text(value).FontSize(12).Bold();
            column.Item().AlignCenter().Text(label).FontSize(6).FontColor(LabelColor);
        });
    }

    private static void WideStatLine(IContainer container, string value, string label)
    {
        container.Border(BorderWidth).Row(row =>
        {
            row.ConstantItem(30).BorderRight(BorderWidth).AlignCenter().AlignMiddle().Padding(3).Text(value).FontSize(11).Bold();
            row.RelativeItem().AlignMiddle().PaddingLeft(4).Text(label).FontSize(7);
        });
    }

    private static void Title(IContainer container, string text) =>
        container.AlignCenter().Text(text.ToUpperInvariant()).FontSize(6.5f).Bold();

    private static void Label(IContainer container, string text) =>
        container.Text(text).FontSize(6).FontColor(LabelColor);

    private static void SectionTitle(IContainer container, string text) =>
        container.BorderBottom(1.5f).PaddingBottom(2).Text(text).FontSize(13).Bold();

    private string ClassAndLevel() =>
        $"{WithChoice(_character.ClassName, _character.SubclassName)} {_character.Level}";

    private static string WithChoice(string name, string? choice) => choice == null ? name : $"{name} ({choice})";

    private static string AbilityName(string abilityId) =>
        AbilityScore.TryNormalize(abilityId, out var id) ? AbilityScore.FromId(id).Name : abilityId;

    private static string Abbreviation(string abilityId) =>
        AbilityScore.TryNormalize(abilityId, out var id) ? AbilityScore.FromId(id).Abbreviation : abilityId;

    private static string SpellsLabel(string? abilityId) =>
        abilityId == null ? "Spells" : $"Spells ({AbilityName(abilityId)})";

    /// <summary>The seed data types tools four ways (tool, artisans_tools, gaming_set, musical-instrument); the sheet lists them as one.</summary>
    private static string ProficiencyHeading(string type) => type switch
    {
        "armor" => "Armour",
        "weapon" => "Weapons",
        _ => "Tools",
    };

    private static string SpellLevelHeading(int level) => level == 0 ? "Cantrips" : $"{Ordinal(level)} level";

    private static string SpellLine(SpellReadModel spell)
    {
        var tags = string.Concat(spell.IsConcentration ? ", concentration" : string.Empty, spell.IsRitual ? ", ritual" : string.Empty);

        return $"{spell.School}{tags} · {spell.CastingTime} · {spell.Range} · {spell.Components} · {spell.Duration}";
    }

    private static string Ordinal(int number) => number switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{number}th",
    };

    private static string Signed(int value) =>
        value >= 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);
}
