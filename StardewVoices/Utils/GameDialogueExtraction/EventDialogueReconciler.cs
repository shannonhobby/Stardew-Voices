using StardewVoices.FileGeneration.Models;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    /// <summary>
    /// Pass 2 reconciler: collapses duplicate audio-file names that arise when
    /// branching event paths (/fork, /switchEvent) independently reach the same
    /// (character, dialogue) pair and each generate a distinct name.
    ///
    /// Operates on raw List&lt;EventLine&gt; records produced by Pass 1
    /// (EventDialogueExtractor.GetEventDialogue). Never touches Pass 1 logic.
    ///
    /// Step A — BuildEventGroupsForLocation
    ///   Walks each root event's script for /fork and /switchEvent commands
    ///   (transitively) and groups the root with every fork/response event ID it
    ///   can reach. Logs the mapping so it can be sanity-checked.
    ///
    /// Step B — Reconcile
    ///   Within each group, finds (Character, Dialogue) pairs that appear more
    ///   than once and collapses later occurrences to the first-seen canonical
    ///   LineId. Lines in different event groups are never deduped.
    /// </summary>
    public static class EventDialogueReconciler
    {
        // ── Step A: Build event groups ────────────────────────────────────────────

        /// <summary>
        /// Returns the event groups for a single location: root event ID mapped to
        /// the list of fork/response event IDs transitively reachable from it.
        ///
        /// Root events have '/' in their raw dictionary key (e.g.
        /// "2481135/f Alex 1000/t 900 1600"). Response/fork events have no '/'.
        ///
        /// The full raw key is included in the log so you can read the event
        /// conditions alongside the fork targets.
        /// </summary>
        public static Dictionary<string, List<string>> BuildEventGroupsForLocation(
            string locationName,
            Dictionary<string, string> locationData)
        {
            var groups = new Dictionary<string, List<string>>();

            foreach (var kvp in locationData)
            {
                if (!kvp.Key.Contains('/')) continue;      // skip response-only events

                string rootId = kvp.Key.Split('/')[0];
                if (groups.ContainsKey(rootId)) continue;  // root already processed

                var reached = new HashSet<string>();
                CollectForkTargets(locationData, kvp.Value, reached);
                groups[rootId] = reached.ToList();
            }

            // ── Log the mapping so the caller can sanity-check it ────────────────
            ErrorLogger.Info($"[Pass 2] Event groups in '{locationName}':");
            foreach (var (rootId, forkIds) in groups)
            {
                string fullKey = locationData.Keys.FirstOrDefault(k => k.Split('/')[0] == rootId)
                    ?? rootId;
                string forks = forkIds.Count > 0
                    ? string.Join(", ", forkIds.Select(f => $"\"{f}\""))
                    : "(none)";
                ErrorLogger.Info($"  \"{fullKey}\" → [{forks}]");
            }

            return groups;
        }

        // ── Step B: Deduplicate within groups ────────────────────────────────────

        /// <summary>
        /// Deduplicates LineIds across forked branches of the same logical event.
        ///
        /// A line is a duplicate only when ALL of the following match:
        ///   • Same event group  (root + all transitively-reachable forks)
        ///   • Same Character    (exact string)
        ///   • Exact Dialogue    (no normalisation — punctuation, emotion tags such
        ///     as "$s"/"$h", and page-break tokens like "#$b#" are all significant)
        ///
        /// First-seen wins. Later occurrences are renamed to the canonical LineId.
        /// Lines in different event groups are NEVER deduped, even if identical.
        /// </summary>
        /// <param name="linesByEventKey">
        /// Raw Pass 1 output keyed by "LocationName/eventId" (e.g. "Town/2481135").
        /// </param>
        /// <param name="getLocationData">
        /// Returns the full event-script dictionary for a given location name.
        /// Used to build the fork graph without re-running extraction.
        /// </param>
        /// <returns>
        /// reconciledLines — same structure as input, with duplicate LineIds replaced.
        /// collapseLog     — old LineId → canonical LineId for every renamed entry.
        /// </returns>
        public static (
            Dictionary<string, List<EventLine>> reconciledLines,
            Dictionary<string, string> collapseLog
        ) Reconcile(
            Dictionary<string, List<EventLine>> linesByEventKey,
            Func<string, Dictionary<string, string>> getLocationData)
        {
            // Shallow-copy per-event lists so we don't mutate the caller's data
            var reconciled = linesByEventKey.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList());

            var collapseLog = new Dictionary<string, string>();

            // Group event keys by location name (first segment before '/')
            var byLocation = reconciled.Keys
                .Select(k => k.Split('/', 2))
                .Where(p => p.Length == 2)
                .Select(p => (location: p[0], eventId: p[1]))
                .GroupBy(t => t.location)
                .ToDictionary(g => g.Key, g => g.Select(t => t.eventId).ToList());

            foreach (var (locationName, eventIds) in byLocation)
            {
                Dictionary<string, string> locationData = getLocationData(locationName);
                if (locationData == null) continue;

                // Step A: build (and log) the fork graph for this location
                var groups = BuildEventGroupsForLocation(locationName, locationData);

                // Step B: dedup within each group
                DeduplicateLocation(locationName, eventIds, groups, reconciled, collapseLog);
            }

            return (reconciled, collapseLog);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private static void DeduplicateLocation(
            string locationName,
            List<string> presentEventIds,
            Dictionary<string, List<string>> groups,
            Dictionary<string, List<EventLine>> lines,
            Dictionary<string, string> collapseLog)
        {
            // Build a fast lookup: eventId → its root event ID
            var eventToRoot = new Dictionary<string, string>();
            foreach (var (rootId, forkIds) in groups)
            {
                eventToRoot[rootId] = rootId;
                foreach (string forkId in forkIds)
                    eventToRoot[forkId] = rootId;
            }

            var processedRoots = new HashSet<string>();

            foreach (string eventId in presentEventIds)
            {
                if (!eventToRoot.TryGetValue(eventId, out string rootId)) continue;
                if (!processedRoots.Add(rootId)) continue;  // group already handled

                // Collect event keys for this group: root first, then forks in order
                var groupKeys = new List<string>();
                string rootKey = $"{locationName}/{rootId}";
                if (lines.ContainsKey(rootKey))
                    groupKeys.Add(rootKey);

                foreach (string forkId in groups[rootId])
                {
                    string forkKey = $"{locationName}/{forkId}";
                    if (lines.ContainsKey(forkKey))
                        groupKeys.Add(forkKey);
                }

                if (groupKeys.Count < 2) continue;  // single-event group: nothing to dedup

                // Walk all lines in root-first order; first occurrence of
                // (Character, Dialogue) establishes the canonical LineId.
                var seen = new Dictionary<(string character, string dialogue), string>();

                foreach (string key in groupKeys)
                {
                    var eventLines = lines[key];
                    for (int i = 0; i < eventLines.Count; i++)
                    {
                        EventLine line = eventLines[i];
                        var dedupeKey = (line.Character, line.Dialogue);

                        if (!seen.TryGetValue(dedupeKey, out string canonicalId))
                        {
                            seen[dedupeKey] = line.LineId;
                        }
                        else if (line.LineId != canonicalId)
                        {
                            collapseLog[line.LineId] = canonicalId;
                            eventLines[i] = line with { LineId = canonicalId };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursively collects all event IDs reachable from <paramref name="script"/>
        /// via /fork and /switchEvent commands, adding them to <paramref name="reached"/>.
        /// Cycles are handled by the HashSet — an ID is only followed once.
        /// </summary>
        private static void CollectForkTargets(
            Dictionary<string, string> locationData,
            string script,
            HashSet<string> reached)
        {
            foreach (string command in script.Split('/'))
            {
                string trimmed = command.Trim();
                string targetId = null;

                if (trimmed.StartsWith("fork "))
                    targetId = trimmed.Substring(5).Trim().Split(' ')[0];
                else if (trimmed.StartsWith("switchEvent "))
                    targetId = trimmed.Substring(12).Trim().Split(' ')[0];

                if (string.IsNullOrEmpty(targetId)) continue;
                if (!locationData.ContainsKey(targetId)) continue;
                if (!reached.Add(targetId)) continue;  // already visited; prevents cycles

                CollectForkTargets(locationData, locationData[targetId], reached);
            }
        }
    }
}
