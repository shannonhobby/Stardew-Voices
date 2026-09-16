using System.Collections.Generic;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    /// <summary>
    /// Hand-curated LineId renames for duplicate lines that EventDialogueReconciler's
    /// fork-reachability pass cannot see, because the two occurrences live in different
    /// event roots (often different locations entirely) with no /fork or /switchEvent
    /// path connecting them — e.g. the same scene witnessed from one location and
    /// triggered directly from another, or the same dialogue tree reached via two
    /// different prerequisite choices.
    ///
    /// Applied by EventDialogueReconciler as Step C, after the fork-based Step A/B pass.
    /// Each entry is a direct LineId -> canonical LineId rename, not a text match, so it
    /// survives dialogue text edits/localization and can't accidentally catch an
    /// unrelated line that happens to share text.
    ///
    /// Canonicalization rules used when building this table:
    ///   - Between a "response_&lt;location&gt;_&lt;eventName&gt;_..." id and a bare
    ///     "&lt;Location&gt;_&lt;eventId&gt;_..." id, the bare location-prefixed id wins.
    ///   - Where duplicates arise purely from a superficial variant (song genre, dialogue
    ///     "class" choice) baked into the id, that variant is stripped entirely to produce
    ///     a new shared canonical id — none of the four/two original ids "win".
    ///
    /// A canonical value must never also appear as a key (no rename chains).
    /// </summary>
    public static class EventDialogueManualOverrides
    {
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            // ── Shane — Saloon "witnessed" vs Town "triggered" (shaneSaloon2) ──────
            ["response_Saloon_wewereworried_Shane_event1_sv2"] = "Town_3917586_shaneSaloon2_Shane_event11_sv2",
            ["response_Saloon_wewereworried_Shane_event2"]     = "Town_3917586_shaneSaloon2_Shane_event12",
            ["response_Saloon_wewereworried_Shane_event3"]     = "Town_3917586_shaneSaloon2_Shane_event13",
            ["response_Saloon_wewereworried_Shane_endDialogue4"] = "Town_3917586_shaneSaloon2_Shane_endDialogue14",

            // ── Harvey — HarveyRoom "normal" vs Hospital "triggered" ───────────────
            ["response_HarveyRoom_normal_Harvey_event5"]     = "Hospital_571102_2000_Harvey_event10",
            ["response_HarveyRoom_normal_Harvey_event6"]     = "Hospital_571102_2000_Harvey_event11",
            ["response_HarveyRoom_normal_Harvey_event7"]     = "Hospital_571102_2000_Harvey_event12",
            ["response_HarveyRoom_normal_Harvey_event7_sv1"] = "Hospital_571102_2000_Harvey_event12_sv1",
            ["response_HarveyRoom_normal_Harvey_event8"]     = "Hospital_571102_2000_Harvey_event13",

            // ── Leah — Forest "sunny" (54) vs "choseInternet" branch ───────────────
            ["response_Forest_choseInternet_Leah_event2_sv1"]        = "Forest_54_sunny_Leah_event8_sv1",
            ["response_Forest_choseInternet_Leah_event3"]            = "Forest_54_sunny_Leah_event9",
            ["response_Forest_choseInternet_Leah_event4_female_farmer"] = "Forest_54_sunny_Leah_event10_female_farmer",
            ["response_Forest_choseInternet_Leah_event5"]            = "Forest_54_sunny_Leah_event11",
            ["response_Forest_choseInternet_Leah_event6"]            = "Forest_54_sunny_Leah_event12",
            ["response_Forest_choseInternet_Leah_endDialogue7"]      = "Forest_54_sunny_Leah_endDialogue13",

            // ── Elliott — ArchaeologyHouse book reading, reached 3 ways ────────────
            ["response_ArchaeologyHouse_mysteryBook_Elliott_event2"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event6",
            ["response_ArchaeologyHouse_romanceBook_Elliott_event2"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event6",
            ["response_ArchaeologyHouse_mysteryBook_Elliott_event5"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event9",
            ["response_ArchaeologyHouse_romanceBook_Elliott_event5"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event9",
            ["response_ArchaeologyHouse_mysteryBook_Elliott_event4"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event8",
            ["response_ArchaeologyHouse_romanceBook_Elliott_event4"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event8",
            ["response_ArchaeologyHouse_mysteryBook_Elliott_event6"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event10",
            ["response_ArchaeologyHouse_romanceBook_Elliott_event6"]     = "ArchaeologyHouse_1848481_elliottReading_Elliott_event10",
            ["response_ArchaeologyHouse_mysteryBook_Elliott_event6_sv2"] = "ArchaeologyHouse_1848481_elliottReading_Elliott_event10_sv2",
            ["response_ArchaeologyHouse_romanceBook_Elliott_event6_sv2"] = "ArchaeologyHouse_1848481_elliottReading_Elliott_event10_sv2",

            // ── SebastianRoom — class-choice dialogue, reached via healer/warrior branch ──
            ["response_SebastianRoom_healer_Sam_event1"] = "SebastianRoom_27_Sebastian_Sam_event2",
            // Sebastian's own reaction line has no bare location-prefixed id on either side,
            // so the "class" (healer/warrior) is stripped from the id instead, same treatment
            // as the battle-of-the-bands genre strip below.
            ["response_SebastianRoom_healer_Sebastian_event1_sv1"]  = "response_SebastianRoom_Sebastian_event1_sv1",
            ["response_SebastianRoom_warrior_Sebastian_event1_sv1"] = "response_SebastianRoom_Sebastian_event1_sv1",

            // ── Sam — SamHouse "stayPut" vs Town "triggered" (samMessage) ──────────
            ["response_SamHouse_stayPut_Sam_event2"] = "Town_233104_samMessage_Sam_event12",

            // ── Battle of the bands — song genre stripped from the id entirely ─────
            // Emily
            ["response_Temp_heavy_Emily_event1_prereq_1"]     = "response_Temp_Emily_event1_prereq_1",
            ["response_Temp_honkytonk_Emily_event1_prereq_1"] = "response_Temp_Emily_event1_prereq_1",
            ["response_Temp_poppy_Emily_event1_prereq_1"]     = "response_Temp_Emily_event1_prereq_1",
            ["response_Temp_techno_Emily_event1_prereq_1"]    = "response_Temp_Emily_event1_prereq_1",
            ["response_Temp_heavy_Emily_event1_prereq_2"]     = "response_Temp_Emily_event1_prereq_2",
            ["response_Temp_honkytonk_Emily_event1_prereq_2"] = "response_Temp_Emily_event1_prereq_2",
            ["response_Temp_poppy_Emily_event1_prereq_2"]     = "response_Temp_Emily_event1_prereq_2",
            ["response_Temp_techno_Emily_event1_prereq_2"]    = "response_Temp_Emily_event1_prereq_2",

            // Leah
            ["response_Temp_heavy_Leah_event1_prereq_1"]      = "response_Temp_Leah_event1_prereq_1",
            ["response_Temp_poppy_Leah_event1_prereq_1"]      = "response_Temp_Leah_event1_prereq_1",
            ["response_Temp_techno_Leah_event1_prereq_1"]     = "response_Temp_Leah_event1_prereq_1",
            ["response_Temp_heavy_Leah_event1_prereq_2"]      = "response_Temp_Leah_event1_prereq_2",
            ["response_Temp_honkytonk_Leah_event1_prereq_2"]  = "response_Temp_Leah_event1_prereq_2",
            ["response_Temp_poppy_Leah_event1_prereq_2"]      = "response_Temp_Leah_event1_prereq_2",
            ["response_Temp_techno_Leah_event1_prereq_2"]     = "response_Temp_Leah_event1_prereq_2",

            // Penny
            ["response_Temp_heavy_Penny_event1"]     = "response_Temp_Penny_event1",
            ["response_Temp_honkytonk_Penny_event1"] = "response_Temp_Penny_event1",
            ["response_Temp_poppy_Penny_event1"]     = "response_Temp_Penny_event1",
            ["response_Temp_techno_Penny_event1"]    = "response_Temp_Penny_event1",

            // Sam
            ["response_Temp_heavy_Sam_event2_prereq_1"]     = "response_Temp_Sam_event2_prereq_1",
            ["response_Temp_honkytonk_Sam_event2_prereq_1"] = "response_Temp_Sam_event2_prereq_1",
            ["response_Temp_poppy_Sam_event2_prereq_1"]     = "response_Temp_Sam_event2_prereq_1",
            ["response_Temp_techno_Sam_event2_prereq_1"]    = "response_Temp_Sam_event2_prereq_1",
            ["response_Temp_heavy_Sam_event2_prereq_2"]     = "response_Temp_Sam_event2_prereq_2",
            ["response_Temp_honkytonk_Sam_event2_prereq_2"] = "response_Temp_Sam_event2_prereq_2",
            ["response_Temp_poppy_Sam_event2_prereq_2"]     = "response_Temp_Sam_event2_prereq_2",
            ["response_Temp_techno_Sam_event2_prereq_2"]    = "response_Temp_Sam_event2_prereq_2",
            ["response_Temp_heavy_Sam_event2_prereq_3"]     = "response_Temp_Sam_event2_prereq_3",
            ["response_Temp_honkytonk_Sam_event2_prereq_3"] = "response_Temp_Sam_event2_prereq_3",
            ["response_Temp_poppy_Sam_event2_prereq_3"]     = "response_Temp_Sam_event2_prereq_3",
            ["response_Temp_techno_Sam_event2_prereq_3"]    = "response_Temp_Sam_event2_prereq_3",
            ["response_Temp_heavy_Sam_event2_prereq_4"]     = "response_Temp_Sam_event2_prereq_4",
            ["response_Temp_honkytonk_Sam_event2_prereq_4"] = "response_Temp_Sam_event2_prereq_4",
            ["response_Temp_poppy_Sam_event2_prereq_4"]     = "response_Temp_Sam_event2_prereq_4",
            ["response_Temp_techno_Sam_event2_prereq_4"]    = "response_Temp_Sam_event2_prereq_4",
            ["response_Temp_heavy_Sam_event3"]     = "response_Temp_Sam_event3",
            ["response_Temp_honkytonk_Sam_event3"] = "response_Temp_Sam_event3",
            ["response_Temp_poppy_Sam_event3"]     = "response_Temp_Sam_event3",
            ["response_Temp_techno_Sam_event3"]    = "response_Temp_Sam_event3",
            ["response_Temp_heavy_Sam_event3_sv1_female_farmer"]     = "response_Temp_Sam_event3_sv1_female_farmer",
            ["response_Temp_honkytonk_Sam_event3_sv1_female_farmer"] = "response_Temp_Sam_event3_sv1_female_farmer",
            ["response_Temp_poppy_Sam_event3_sv1_female_farmer"]     = "response_Temp_Sam_event3_sv1_female_farmer",
            ["response_Temp_techno_Sam_event3_sv1_female_farmer"]    = "response_Temp_Sam_event3_sv1_female_farmer",
            ["response_Temp_heavy_Sam_event3_sv1_male_farmer"]     = "response_Temp_Sam_event3_sv1_male_farmer",
            ["response_Temp_honkytonk_Sam_event3_sv1_male_farmer"] = "response_Temp_Sam_event3_sv1_male_farmer",
            ["response_Temp_poppy_Sam_event3_sv1_male_farmer"]     = "response_Temp_Sam_event3_sv1_male_farmer",
            ["response_Temp_techno_Sam_event3_sv1_male_farmer"]    = "response_Temp_Sam_event3_sv1_male_farmer",
            ["response_Temp_heavy_Sam_event4"]     = "response_Temp_Sam_event4",
            ["response_Temp_honkytonk_Sam_event4"] = "response_Temp_Sam_event4",
            ["response_Temp_poppy_Sam_event4"]     = "response_Temp_Sam_event4",
            ["response_Temp_techno_Sam_event4"]    = "response_Temp_Sam_event4",
            ["response_Temp_heavy_Sam_event5"]     = "response_Temp_Sam_event5",
            ["response_Temp_honkytonk_Sam_event5"] = "response_Temp_Sam_event5",
            ["response_Temp_poppy_Sam_event5"]     = "response_Temp_Sam_event5",
            ["response_Temp_techno_Sam_event5"]    = "response_Temp_Sam_event5",
            // Closing line also said verbatim in BusStop's own pre-show scene — a real
            // location-prefixed original exists here, so it wins over a genre-stripped id.
            ["response_Temp_heavy_Sam_endDialogue6"]     = "BusStop_4081148_1900_Sam_endDialogue6",
            ["response_Temp_honkytonk_Sam_endDialogue6"] = "BusStop_4081148_1900_Sam_endDialogue6",
            ["response_Temp_poppy_Sam_endDialogue6"]     = "BusStop_4081148_1900_Sam_endDialogue6",
            ["response_Temp_techno_Sam_endDialogue6"]    = "BusStop_4081148_1900_Sam_endDialogue6",

            // Shane
            ["response_Temp_heavy_Shane_event1_prereq_1"]     = "response_Temp_Shane_event1_prereq_1",
            ["response_Temp_honkytonk_Shane_event1_prereq_1"] = "response_Temp_Shane_event1_prereq_1",
            ["response_Temp_poppy_Shane_event1_prereq_1"]     = "response_Temp_Shane_event1_prereq_1",
            ["response_Temp_techno_Shane_event1_prereq_1"]    = "response_Temp_Shane_event1_prereq_1",
            ["response_Temp_heavy_Shane_event1_prereq_2"]     = "response_Temp_Shane_event1_prereq_2",
            ["response_Temp_honkytonk_Shane_event1_prereq_2"] = "response_Temp_Shane_event1_prereq_2",
            ["response_Temp_poppy_Shane_event1_prereq_2"]     = "response_Temp_Shane_event1_prereq_2",
            ["response_Temp_techno_Shane_event1_prereq_2"]    = "response_Temp_Shane_event1_prereq_2",
        };
    }
}
