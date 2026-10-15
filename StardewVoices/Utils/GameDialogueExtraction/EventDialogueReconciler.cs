using StardewVoices.FileGeneration.Models;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    /// <summary>
    /// Pass 2 reconciler: collapses duplicate lines that arise when
    /// branching event paths (/fork, /switchEvent) independently reach the same
    /// (character, dialogue) pair and each generate a distinct name. This is a problem
    /// as it requires multiple versions of the same audio files to exist in order to 
    /// be named correctly.
    ///
    /// Operates on records produced by Pass 1
    /// (FileGenerator.GenerateFiles). Never touches Pass 1 logic.
    ///
    /// Step A — BuildEventGroups
    ///   Follows each root event's /fork and /switchEvent commands (transitively) and
    ///   groups the root with every fork/response event it can reach — including ones
    ///   in another location's file, reached after a changeLocation (see
    ///   EventDialogueExtractor.GetForks). Logs the mapping so it can be sanity-checked.
    ///
    /// Step B — Reconcile
    ///   Within each group, finds (Character, Dialogue) pairs that appear more
    ///   than once and collapses later occurrences to the first-seen canonical
    ///   LineId. Lines in different event groups are never deduped.
    ///
    /// Step C — ApplyManualOverrides
    ///   Renames LineIds listed in EventDialogueManualOverrides.Map to their
    ///   canonical id, across the whole dataset (not scoped to one location).
    ///   Covers duplicate lines that Step A/B structurally cannot see because
    ///   no /fork or /switchEvent path connects the two occurrences — e.g. the
    ///   same scene witnessed from one location and triggered directly from
    ///   another. See that file for the hand-curated table and the rules used
    ///   to pick each canonical id.
    /// </summary>
    public static class EventDialogueReconciler
    {
        // ── Step A: Build event groups ────────────────────────────────────────────

        /// <summary>
        /// Returns the event groups for a single location: root event key
        /// ("Location/eventId") mapped to the "Location/eventId" keys of every fork/response
        /// event transitively reachable from it — which may live in other locations.
        ///
        /// Root events have '/' in their raw dictionary key (e.g.
        /// "2481135/f Alex 1000/t 900 1600"). Response/fork events have no '/'.
        ///
        /// The full raw key is included in the log so you can read the event
        /// conditions alongside the fork targets.
        /// </summary>
        public static Dictionary<string, List<string>> BuildEventGroupsForLocation(
            string locationName,
            Dictionary<string, string> locationData,
            Func<string, IReadOnlyList<EventFork>> getForks)
        {
            var groups = new Dictionary<string, List<string>>();

            foreach (var kvp in locationData)
            {
                if (!kvp.Key.Contains('/')) continue;      // skip response-only events

                string rootKey = $"{locationName}/{kvp.Key.Split('/')[0]}";
                if (groups.ContainsKey(rootKey)) continue;  // root already processed

                var visited = new HashSet<string> { rootKey };
                var reached = new List<string>();
                CollectForkTargets(rootKey, getForks, visited, reached);
                groups[rootKey] = reached;
            }

            // ── Log the mapping so the caller can sanity-check it ────────────────
            ErrorLogger.Info($"[Pass 2] Event groups in '{locationName}':");
            foreach (var (rootKey, forkKeys) in groups)
            {
                string rootId = rootKey.Split('/', 2)[1];
                string fullKey = locationData.Keys.FirstOrDefault(k => k.Split('/')[0] == rootId)
                    ?? rootId;
                string forks = forkKeys.Count > 0
                    ? string.Join(", ", forkKeys.Select(f => $"\"{f}\""))
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
        /// First-seen lineId wins. Later occurrences are renamed to the canonical LineId.
        /// Lines in different event groups are NEVER deduped, even if identical.
        /// </summary>
        /// <param name="linesByEventKey">
        /// Raw Pass 1 output keyed by "LocationName/eventId" (e.g. "Town/2481135").
        /// </param>
        /// <param name="getLocationData">
        /// Returns the full event-script dictionary for a given location name.
        /// Used to find each location's root events.
        /// </param>
        /// <param name="getForks">
        /// Returns the resolved fork/switchEvent targets of an event ("Location/eventId"),
        /// already following changeLocation — see EventDialogueExtractor.GetForks.
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
            Func<string, Dictionary<string, string>> getLocationData,
            Func<string, IReadOnlyList<EventFork>> getForks)
        {
            // Shallow-copy per-event lists so we don't mutate the caller's data
            var reconciled = linesByEventKey.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList());

            var collapseLog = new Dictionary<string, string>();

            // Step A: build (and log) the fork graph, one location's roots at a time.
            // A group's forks can reach into other locations, so groups are merged into
            // one map and deduped together rather than per location.
            var groups = new Dictionary<string, List<string>>();
            foreach (string locationName in reconciled.Keys.Select(k => k.Split('/', 2)[0]).Distinct())
            {
                Dictionary<string, string> locationData = getLocationData(locationName);
                if (locationData == null) continue;

                foreach (var (rootKey, forkKeys) in BuildEventGroupsForLocation(locationName, locationData, getForks))
                    groups[rootKey] = forkKeys;
            }

            // Step B: dedup within each group
            Deduplicate(groups, reconciled, collapseLog);

            // Step C: apply hand-curated cross-location overrides
            ApplyManualOverrides(reconciled, collapseLog);

            return (reconciled, collapseLog);
        }

        // ── Step C: manual cross-location overrides ─────────────────────────────

        /// <summary>
        /// Renames every LineId found in EventDialogueManualOverrides.Map to its
        /// canonical value, across all locations. Unlike Step B this isn't scoped to
        /// a fork-reachability group — these are duplicates the extractor has no
        /// structural way to connect.
        /// </summary>
        private static void ApplyManualOverrides(
            Dictionary<string, List<EventLine>> lines,
            Dictionary<string, string> collapseLog)
        {
            var overrides = EventDialogueManualOverrides.Map;
            var seenKeys = new HashSet<string>();

            foreach (var eventLines in lines.Values)
            {
                for (int i = 0; i < eventLines.Count; i++)
                {
                    EventLine line = eventLines[i];
                    if (!overrides.TryGetValue(line.LineId, out string canonicalId)) continue;

                    seenKeys.Add(line.LineId);
                    collapseLog[line.LineId] = canonicalId;
                    eventLines[i] = line with { LineId = canonicalId };
                }
            }

            var presentIds = new HashSet<string>(lines.Values.SelectMany(eventLines => eventLines).Select(line => line.LineId));
            foreach (string key in overrides.Keys)
            {
                if (seenKeys.Contains(key)) continue;

                // Step B can collapse a key before Step C sees it, when fork reachability
                // already connects the two occurrences. Only warn if the rename didn't
                // land either way.
                if (collapseLog.TryGetValue(key, out _) && presentIds.Contains(overrides[key]))
                    ErrorLogger.Info($"[Pass 2, Step C] Manual override \"{key}\" already covered by fork dedup — can be removed.");
                else
                    ErrorLogger.Warn($"[Pass 2, Step C] Manual override key not found in extracted data (stale mapping?): \"{key}\"");
            }

            ErrorLogger.Info($"[Pass 2, Step C] Applied {seenKeys.Count}/{overrides.Count} manual overrides.");
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private static void Deduplicate(
            Dictionary<string, List<string>> groups,
            Dictionary<string, List<EventLine>> lines,
            Dictionary<string, string> collapseLog)
        {
            // Build a fast lookup: event key → its root event key
            var eventToRoot = new Dictionary<string, string>();
            foreach (var (rootKey, forkKeys) in groups)
            {
                eventToRoot[rootKey] = rootKey;
                foreach (string forkKey in forkKeys)
                    eventToRoot[forkKey] = rootKey;
            }

            var processedRoots = new HashSet<string>();

            foreach (string eventKey in lines.Keys.ToList())
            {
                if (!eventToRoot.TryGetValue(eventKey, out string rootKey)) continue;
                if (!processedRoots.Add(rootKey)) continue;  // group already handled

                // Collect event keys for this group: root first, then forks in order
                var groupKeys = new List<string>();
                if (lines.ContainsKey(rootKey))
                    groupKeys.Add(rootKey);

                foreach (string forkKey in groups[rootKey])
                {
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
        /// Recursively collects every event key reachable from <paramref name="eventKey"/>
        /// via /fork and /switchEvent, in discovery order, adding them to <paramref name="reached"/>.
        /// Cycles are handled by <paramref name="visited"/> — a key is only followed once.
        /// </summary>
        private static void CollectForkTargets(
            string eventKey,
            Func<string, IReadOnlyList<EventFork>> getForks,
            HashSet<string> visited,
            List<string> reached)
        {
            foreach (EventFork fork in getForks(eventKey))
            {
                if (!visited.Add(fork.TargetEventKey)) continue;  // already visited; prevents cycles

                reached.Add(fork.TargetEventKey);
                CollectForkTargets(fork.TargetEventKey, getForks, visited, reached);
            }
        }
    }
}
