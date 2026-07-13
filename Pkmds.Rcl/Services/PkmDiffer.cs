namespace Pkmds.Rcl.Services;

/// <summary>
/// Computes the field-level differences between two <see cref="PKM" /> instances. Used
/// after a successful legalization to surface "what was changed" to the user.
/// </summary>
/// <remarks>
/// PKHeX.Core does not expose a diff/changelog from <see cref="LegalityAnalysis" /> or
/// any of the legalize entry points — the only product is the final PKM. This service
/// reconstructs the diff by inspecting matching properties on the before/after PKMs.
/// Format-gated fields (Hyper Training, AVs, GVs, Tera type, etc.) are checked via
/// interface tests so the differ doesn't blow up on cross-format comparisons.
/// </remarks>
public static class PkmDiffer
{
    /// <summary>
    /// Returns the set of human-meaningful differences between <paramref name="before" />
    /// and <paramref name="after" />. Returns <see cref="LegalizationChanges.Empty" /> if
    /// either argument is null or no fields differ.
    /// </summary>
    public static LegalizationChanges Diff(PKM? before, PKM? after)
    {
        if (before is null || after is null)
        {
            return LegalizationChanges.Empty;
        }

        var changes = new List<LegalizationChange>();

        AddIdentity(before, after, changes);
        AddOrigin(before, after, changes);
        AddBattle(before, after, changes);
        AddStats(before, after, changes);
        AddCosmetic(before, after, changes);
        AddInternal(before, after, changes);

        return changes.Count == 0
            ? LegalizationChanges.Empty
            : new LegalizationChanges(changes);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Identity
    // ──────────────────────────────────────────────────────────────────────

    private static void AddIdentity(PKM b, PKM a, List<LegalizationChange> changes)
    {
        if (b.Species != a.Species)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Identity, "Species",
                SafeNameLookup.Species(b.Species), SafeNameLookup.Species(a.Species)));
        }

        if (b.Form != a.Form)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Identity, "Form",
                b.Form.ToString(CultureInfo.InvariantCulture),
                a.Form.ToString(CultureInfo.InvariantCulture)));
        }

        if (!string.Equals(b.Nickname, a.Nickname, StringComparison.Ordinal))
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Identity, "Nickname", b.Nickname, a.Nickname));
        }

        if (b.IsNicknamed != a.IsNicknamed)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Identity, "Is Nicknamed",
                b.IsNicknamed.ToString(), a.IsNicknamed.ToString()));
        }

        if (b.Gender != a.Gender)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Identity, "Gender",
                FormatGender(b.Gender), FormatGender(a.Gender)));
        }

        if (b.Language != a.Language)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Identity, "Language",
                FormatLanguage(b.Language), FormatLanguage(a.Language)));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Origin
    // ──────────────────────────────────────────────────────────────────────

    private static void AddOrigin(PKM b, PKM a, List<LegalizationChange> changes)
    {
        if (b.Ball != a.Ball)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Ball",
                FormatBall(b.Ball), FormatBall(a.Ball)));
        }

        if (b.MetLocation != a.MetLocation)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Met Location",
                FormatLocation(b, isEgg: false), FormatLocation(a, isEgg: false)));
        }

        if (b.MetLevel != a.MetLevel)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Met Level",
                b.MetLevel.ToString(CultureInfo.InvariantCulture),
                a.MetLevel.ToString(CultureInfo.InvariantCulture)));
        }

        if (b.MetDate != a.MetDate)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Met Date",
                FormatDate(b.MetDate), FormatDate(a.MetDate)));
        }

        if (b.EggLocation != a.EggLocation)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Egg Location",
                FormatLocation(b, isEgg: true), FormatLocation(a, isEgg: true)));
        }

        if (b.EggMetDate != a.EggMetDate)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Egg Date",
                FormatDate(b.EggMetDate), FormatDate(a.EggMetDate)));
        }

        if (!string.Equals(b.OriginalTrainerName, a.OriginalTrainerName, StringComparison.Ordinal))
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "OT Name",
                b.OriginalTrainerName, a.OriginalTrainerName));
        }

        if (b.OriginalTrainerGender != a.OriginalTrainerGender)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "OT Gender",
                FormatGender(b.OriginalTrainerGender), FormatGender(a.OriginalTrainerGender)));
        }

        if (b.TID16 != a.TID16)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "TID",
                b.TID16.ToString(CultureInfo.InvariantCulture),
                a.TID16.ToString(CultureInfo.InvariantCulture)));
        }

        if (b.SID16 != a.SID16)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "SID",
                b.SID16.ToString(CultureInfo.InvariantCulture),
                a.SID16.ToString(CultureInfo.InvariantCulture)));
        }

        if (b.Version != a.Version)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Origin Game",
                b.Version.ToString(), a.Version.ToString()));
        }

        if (b.FatefulEncounter != a.FatefulEncounter)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Origin, "Fateful Encounter",
                b.FatefulEncounter.ToString(), a.FatefulEncounter.ToString()));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Battle
    // ──────────────────────────────────────────────────────────────────────

    private static void AddBattle(PKM b, PKM a, List<LegalizationChange> changes)
    {
        AddMoveDiff(changes, "Move 1", b.Move1, a.Move1, b.Move1_PPUps, a.Move1_PPUps);
        AddMoveDiff(changes, "Move 2", b.Move2, a.Move2, b.Move2_PPUps, a.Move2_PPUps);
        AddMoveDiff(changes, "Move 3", b.Move3, a.Move3, b.Move3_PPUps, a.Move3_PPUps);
        AddMoveDiff(changes, "Move 4", b.Move4, a.Move4, b.Move4_PPUps, a.Move4_PPUps);

        AddRelearnDiff(changes, "Relearn 1", b.RelearnMove1, a.RelearnMove1);
        AddRelearnDiff(changes, "Relearn 2", b.RelearnMove2, a.RelearnMove2);
        AddRelearnDiff(changes, "Relearn 3", b.RelearnMove3, a.RelearnMove3);
        AddRelearnDiff(changes, "Relearn 4", b.RelearnMove4, a.RelearnMove4);

        if (b.HeldItem != a.HeldItem)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Battle, "Held Item",
                SafeNameLookup.Item(b.HeldItem), SafeNameLookup.Item(a.HeldItem)));
        }

        if (b.Ability != a.Ability)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Battle, "Ability",
                SafeNameLookup.Ability(b.Ability), SafeNameLookup.Ability(a.Ability)));
        }

        if (b.AbilityNumber != a.AbilityNumber)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Battle, "Ability Slot",
                FormatAbilitySlot(b.AbilityNumber), FormatAbilitySlot(a.AbilityNumber)));
        }

        if (b is ITeraType bt && a is ITeraType at)
        {
            if (bt.TeraTypeOriginal != at.TeraTypeOriginal)
            {
                changes.Add(new LegalizationChange(
                    LegalizationChangeCategory.Battle, "Tera Type (Original)",
                    bt.TeraTypeOriginal.ToString(), at.TeraTypeOriginal.ToString()));
            }

            if (bt.TeraTypeOverride != at.TeraTypeOverride)
            {
                changes.Add(new LegalizationChange(
                    LegalizationChangeCategory.Battle, "Tera Type (Override)",
                    bt.TeraTypeOverride.ToString(), at.TeraTypeOverride.ToString()));
            }
        }
    }

    private static void AddMoveDiff(
        List<LegalizationChange> changes, string label,
        ushort beforeMove, ushort afterMove, int beforePpUps, int afterPpUps)
    {
        if (beforeMove != afterMove)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Battle, label,
                SafeNameLookup.Move(beforeMove), SafeNameLookup.Move(afterMove)));
        }
        else if (beforePpUps != afterPpUps)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Battle, $"{label} PP Ups",
                beforePpUps.ToString(CultureInfo.InvariantCulture),
                afterPpUps.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void AddRelearnDiff(
        List<LegalizationChange> changes, string label, ushort beforeMove, ushort afterMove)
    {
        if (beforeMove == afterMove)
        {
            return;
        }

        changes.Add(new LegalizationChange(
            LegalizationChangeCategory.Battle, label,
            SafeNameLookup.Move(beforeMove), SafeNameLookup.Move(afterMove)));
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Stats
    // ──────────────────────────────────────────────────────────────────────

    private static void AddStats(PKM b, PKM a, List<LegalizationChange> changes)
    {
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "HP IV", b.IV_HP, a.IV_HP);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "Atk IV", b.IV_ATK, a.IV_ATK);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "Def IV", b.IV_DEF, a.IV_DEF);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpA IV", b.IV_SPA, a.IV_SPA);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpD IV", b.IV_SPD, a.IV_SPD);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "Spe IV", b.IV_SPE, a.IV_SPE);

        AddIntDiff(changes, LegalizationChangeCategory.Stats, "HP EV", b.EV_HP, a.EV_HP);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "Atk EV", b.EV_ATK, a.EV_ATK);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "Def EV", b.EV_DEF, a.EV_DEF);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpA EV", b.EV_SPA, a.EV_SPA);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpD EV", b.EV_SPD, a.EV_SPD);
        AddIntDiff(changes, LegalizationChangeCategory.Stats, "Spe EV", b.EV_SPE, a.EV_SPE);

        if (b.Nature != a.Nature)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Stats, "Nature",
                SafeNameLookup.Nature((int)b.Nature), SafeNameLookup.Nature((int)a.Nature)));
        }

        if (b.StatAlignment != a.StatAlignment)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Stats, "Stat Alignment",
                SafeNameLookup.Nature((int)b.StatAlignment), SafeNameLookup.Nature((int)a.StatAlignment)));
        }

        if (b is IHyperTrain bh && a is IHyperTrain ah)
        {
            AddBoolDiff(changes, LegalizationChangeCategory.Stats, "Hyper Train HP", bh.HT_HP, ah.HT_HP);
            AddBoolDiff(changes, LegalizationChangeCategory.Stats, "Hyper Train Atk", bh.HT_ATK, ah.HT_ATK);
            AddBoolDiff(changes, LegalizationChangeCategory.Stats, "Hyper Train Def", bh.HT_DEF, ah.HT_DEF);
            AddBoolDiff(changes, LegalizationChangeCategory.Stats, "Hyper Train SpA", bh.HT_SPA, ah.HT_SPA);
            AddBoolDiff(changes, LegalizationChangeCategory.Stats, "Hyper Train SpD", bh.HT_SPD, ah.HT_SPD);
            AddBoolDiff(changes, LegalizationChangeCategory.Stats, "Hyper Train Spe", bh.HT_SPE, ah.HT_SPE);
        }

        if (b is IAwakened bav && a is IAwakened aav)
        {
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "HP AV", bav.AV_HP, aav.AV_HP);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "Atk AV", bav.AV_ATK, aav.AV_ATK);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "Def AV", bav.AV_DEF, aav.AV_DEF);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpA AV", bav.AV_SPA, aav.AV_SPA);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpD AV", bav.AV_SPD, aav.AV_SPD);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "Spe AV", bav.AV_SPE, aav.AV_SPE);
        }

        if (b is IGanbaru bgv && a is IGanbaru agv)
        {
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "HP GV", bgv.GV_HP, agv.GV_HP);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "Atk GV", bgv.GV_ATK, agv.GV_ATK);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "Def GV", bgv.GV_DEF, agv.GV_DEF);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpA GV", bgv.GV_SPA, agv.GV_SPA);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "SpD GV", bgv.GV_SPD, agv.GV_SPD);
            AddIntDiff(changes, LegalizationChangeCategory.Stats, "Spe GV", bgv.GV_SPE, agv.GV_SPE);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Cosmetic
    // ──────────────────────────────────────────────────────────────────────

    private static void AddCosmetic(PKM b, PKM a, List<LegalizationChange> changes)
    {
        if (b.IsShiny != a.IsShiny)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Cosmetic, "Shiny",
                b.IsShiny.ToString(), a.IsShiny.ToString()));
        }

        if (b.OriginalTrainerFriendship != a.OriginalTrainerFriendship)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Cosmetic, "OT Friendship",
                b.OriginalTrainerFriendship.ToString(CultureInfo.InvariantCulture),
                a.OriginalTrainerFriendship.ToString(CultureInfo.InvariantCulture)));
        }

        AddMarkingDiff(b, a, changes);
        AddRibbonDelta(b, a, changes);
    }

    /// <summary>
    /// Names for marking slots 0–5 (Circle, Triangle, Square, Heart, Star, Diamond).
    /// Slot order is the same in every generation that uses <see cref="IAppliedMarkings3" />,
    /// <see cref="IAppliedMarkings4" />, or <see cref="IAppliedMarkings7" />.
    /// </summary>
    private static readonly string[] MarkingSlotNames =
        ["Circle", "Triangle", "Square", "Heart", "Star", "Diamond"];

    private static void AddMarkingDiff(PKM b, PKM a, List<LegalizationChange> changes)
    {
        // Two flavours: Gen 7+ uses MarkingColor (None/Blue/Pink) per slot via
        // IAppliedMarkings<MarkingColor>; Gen 3-6 uses bool per slot via
        // IAppliedMarkings<bool>. Diff each slot individually so the dialog reads
        // "Marking (Triangle): None → Blue" instead of opaque packed-byte hex.
        if (b is IAppliedMarkings<MarkingColor> bColors && a is IAppliedMarkings<MarkingColor> aColors)
        {
            DiffMarkingSlots(bColors, aColors, changes,
                v => v == MarkingColor.None ? "None" : v.ToString());
        }
        else if (b is IAppliedMarkings<bool> bBools && a is IAppliedMarkings<bool> aBools)
        {
            DiffMarkingSlots(bBools, aBools, changes,
                v => v ? "Set" : "Unset");
        }
    }

    private static void DiffMarkingSlots<T>(
        IAppliedMarkings<T> before,
        IAppliedMarkings<T> after,
        List<LegalizationChange> changes,
        Func<T, string> format)
        where T : unmanaged
    {
        var slots = Math.Min(before.MarkingCount, MarkingSlotNames.Length);
        for (var i = 0; i < slots; i++)
        {
            var bv = before.GetMarking(i);
            var av = after.GetMarking(i);
            if (EqualityComparer<T>.Default.Equals(bv, av))
            {
                continue;
            }

            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Cosmetic,
                $"Marking ({MarkingSlotNames[i]})",
                format(bv),
                format(av)));
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "All PKM types and their members are preserved via PreservePkmTypes.xml.")]
    private static void AddRibbonDelta(PKM b, PKM a, List<LegalizationChange> changes)
    {
        // Collapse to a single "added / removed" entry. Listing each ribbon would
        // dominate the dialog because legalize re-derives the full ribbon set.
        // Use RibbonInfo.GetRibbonInfo (reflection-based) for format-aware coverage —
        // it walks every property starting with "Ribbon" on the actual PKM subtype.
        var beforeRibbons = RibbonInfo.GetRibbonInfo(b);
        var afterRibbons = RibbonInfo.GetRibbonInfo(a);
        var afterByName = new Dictionary<string, RibbonInfo>(afterRibbons.Count, StringComparer.Ordinal);
        foreach (var ribbon in afterRibbons)
        {
            afterByName[ribbon.Name] = ribbon;
        }

        var added = 0;
        var removed = 0;
        foreach (var beforeRibbon in beforeRibbons)
        {
            if (!afterByName.TryGetValue(beforeRibbon.Name, out var afterRibbon))
            {
                continue;
            }

            switch (beforeRibbon.Type)
            {
                case RibbonValueType.Boolean
                    when beforeRibbon.HasRibbon != afterRibbon.HasRibbon:
                    if (afterRibbon.HasRibbon)
                    {
                        added++;
                    }
                    else
                    {
                        removed++;
                    }

                    break;
                case RibbonValueType.Byte
                    when beforeRibbon.RibbonCount != afterRibbon.RibbonCount:
                    var delta = afterRibbon.RibbonCount - beforeRibbon.RibbonCount;
                    if (delta > 0)
                    {
                        added += delta;
                    }
                    else
                    {
                        removed += -delta;
                    }

                    break;
            }
        }

        if (added > 0 || removed > 0)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Cosmetic, "Ribbons",
                null,
                $"+{added} / -{removed}"));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────────────────────────────────

    private static void AddInternal(PKM b, PKM a, List<LegalizationChange> changes)
    {
        if (b.PID != a.PID)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Internal, "PID",
                b.PID.ToString("X8", CultureInfo.InvariantCulture),
                a.PID.ToString("X8", CultureInfo.InvariantCulture)));
        }

        if (b.EncryptionConstant != a.EncryptionConstant)
        {
            changes.Add(new LegalizationChange(
                LegalizationChangeCategory.Internal, "Encryption Constant",
                b.EncryptionConstant.ToString("X8", CultureInfo.InvariantCulture),
                a.EncryptionConstant.ToString("X8", CultureInfo.InvariantCulture)));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────

    private static void AddIntDiff(
        List<LegalizationChange> changes, LegalizationChangeCategory category,
        string label, int before, int after)
    {
        if (before == after)
        {
            return;
        }

        changes.Add(new LegalizationChange(
            category, label,
            before.ToString(CultureInfo.InvariantCulture),
            after.ToString(CultureInfo.InvariantCulture)));
    }

    private static void AddBoolDiff(
        List<LegalizationChange> changes, LegalizationChangeCategory category,
        string label, bool before, bool after)
    {
        if (before == after)
        {
            return;
        }

        changes.Add(new LegalizationChange(
            category, label, before.ToString(), after.ToString()));
    }

    private static string FormatGender(byte gender) => gender switch
    {
        0 => "Male",
        1 => "Female",
        2 => "Genderless",
        _ => gender.ToString(CultureInfo.InvariantCulture)
    };

    private static string FormatLanguage(int language)
    {
        if (Enum.IsDefined(typeof(LanguageID), (byte)language))
        {
            return ((LanguageID)language).ToString();
        }

        return language.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatBall(byte ball)
    {
        var balls = GameInfo.Strings.balllist;
        return ball < balls.Length && !string.IsNullOrEmpty(balls[ball])
            ? balls[ball]
            : $"(Ball #{ball:000})";
    }

    private static string FormatLocation(PKM pkm, bool isEgg)
    {
        var locId = isEgg ? pkm.EggLocation : pkm.MetLocation;
        return GameInfo.GetLocationName(isEgg, locId, pkm.Format, pkm.Generation, pkm.Version);
    }

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";

    private static string FormatAbilitySlot(int abilityNumber) => abilityNumber switch
    {
        1 => "1 (primary)",
        2 => "2 (secondary)",
        4 => "Hidden",
        _ => abilityNumber.ToString(CultureInfo.InvariantCulture)
    };
}
