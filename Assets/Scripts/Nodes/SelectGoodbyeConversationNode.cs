using System.Collections.Generic;
using UnityEngine;

namespace VNEngine
{
    /// <summary>
    /// End-of-semester selector:
    /// 1) Prefer any candidate who has reached their "final stage" (derived from StageRouteIndex across stageScenes).
    /// 2) Else pick the candidate with highest progress (max stage reached across stageScenes).
    /// 3) Else fallbackConversation.
    ///
    /// Progress stat keys match CharacterStageRouterNode:
    ///   $"{character} - {sceneName} - Stage"
    /// </summary>
    public class SelectGoodbyeConversationNode : Node
    {
        [System.Serializable]
        public class GoodbyeCandidate
        {
            public Character character;

            [Tooltip("Conversation to start if this candidate is selected.")]
            public ConversationManager goodbyeConversation;

            [Tooltip("Optional label for debugging.")]
            public string debugLabel;
        }

        [Header("Index (source of truth for max authored stage)")]
        public StageRouteIndex stageRouteIndex;

        [Header("Candidates")]
        public GoodbyeCandidate[] candidates;

        [Header("Stage Scan Scope")]
        [Tooltip("Scenes to scan for per-scene stage stats and index routes (e.g., Apartment, Green, Library, Dining Hall).")]
        public string[] stageScenes = new[] { "Apartment", "Green", "Library", "Dining Hall" };

        [Header("Optional gating")]
        [Tooltip("If > 0, treats final stage as the max stage whose unlockWeek <= weekCap. Use this if you want 'reachable this semester'.")]
        public int weekCap = 0;

        [Header("Fallback")]
        public ConversationManager fallbackConversation;

        [Header("Debug")]
        public bool verboseLogs = true;

        public override void Run_Node()
        {
            go_to_next_node = false;

            var selected = SelectConversation(out string reason);

            if (verboseLogs)
                Debug.Log($"[SelectGoodbyeConversationNode] Selected: {(selected ? selected.name : "NULL")} | {reason}", gameObject);

            if (selected != null)
            {
                selected.Start_Conversation();
                Finish_Node();
                return;
            }

            Debug.LogError("[SelectGoodbyeConversationNode] No conversation selected and no fallback provided.", gameObject);
            // Safety: don't dead-end.
            go_to_next_node = true;
            Finish_Node();
        }

        private ConversationManager SelectConversation(out string reason)
        {
            reason = "";

            if (candidates == null || candidates.Length == 0)
            {
                reason = "No candidates configured; using fallback.";
                return fallbackConversation;
            }

            if (stageScenes == null || stageScenes.Length == 0)
            {
                reason = "No stageScenes configured; using fallback.";
                return fallbackConversation;
            }

            // Build final-stage map from StageRouteIndex:
            // finalAuthored[character] = max stage among index routes for the configured scenes (optionally week-gated).
            var finalAuthored = BuildFinalStageMap();

            // Pass 1: choose among candidates who reached their derived final stage.
            int bestFinalIndex = -1;
            float bestFinalProgress = float.NegativeInfinity;

            // Pass 2: choose best overall progress.
            int bestProgressIndex = -1;
            float bestProgress = float.NegativeInfinity;

            bool anyProgress = false;

            for (int i = 0; i < candidates.Length; i++)
            {
                var c = candidates[i];
                if (c == null) continue;

                float progress = GetMaxStageFromStats(c.character);
                if (progress > 0f) anyProgress = true;

                int finalStage = 0;
                finalAuthored.TryGetValue(c.character, out finalStage);

                if (verboseLogs)
                {
                    Debug.Log(
                        $"[SelectGoodbyeConversationNode] {SafeLabel(c, i)} char={c.character} " +
                        $"progress={progress} finalAuthored={finalStage}",
                        gameObject);
                }

                // Track best progress overall
                if (progress > bestProgress)
                {
                    bestProgress = progress;
                    bestProgressIndex = i;
                }

                // Final-stage eligibility: requires that a final stage exists in index (finalStage > 0)
                // and player progress has reached it.
                if (finalStage > 0 && progress >= finalStage)
                {
                    if (progress > bestFinalProgress)
                    {
                        bestFinalProgress = progress;
                        bestFinalIndex = i;
                    }
                }
            }

            if (bestFinalIndex >= 0)
            {
                var c = candidates[bestFinalIndex];
                reason = $"Final-stage winner (reached authored max): {SafeLabel(c, bestFinalIndex)}";
                return c.goodbyeConversation != null ? c.goodbyeConversation : fallbackConversation;
            }

            if (anyProgress && bestProgressIndex >= 0)
            {
                var c = candidates[bestProgressIndex];
                reason = $"Best-progress winner (no one reached authored max): {SafeLabel(c, bestProgressIndex)}";
                return c.goodbyeConversation != null ? c.goodbyeConversation : fallbackConversation;
            }

            reason = "No progress detected with any candidate; using fallback.";
            return fallbackConversation;
        }

        private Dictionary<Character, int> BuildFinalStageMap()
        {
            var map = new Dictionary<Character, int>();

            if (stageRouteIndex == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[SelectGoodbyeConversationNode] stageRouteIndex is null; derived final stages will be 0.", gameObject);
                return map;
            }

            int currentWeek = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));

            // If weekCap is set, use it; else use int.MaxValue (i.e., do not week-gate).
            int effectiveWeekCap = weekCap > 0 ? weekCap : int.MaxValue;

            // If caller sets weekCap, that’s authoritative. If not, we can still use currentWeek
            // if you want "final reachable so far." Right now we treat weekCap==0 as "no gating".
            // If you want "reachable by now" behavior, replace int.MaxValue with currentWeek.
            if (verboseLogs)
            {
                Debug.Log(
                    $"[SelectGoodbyeConversationNode] Building final-stage map. " +
                    $"weekCap={weekCap} effectiveWeekCap={effectiveWeekCap} currentWeek={currentWeek}",
                    gameObject);
            }

            foreach (var scene in stageScenes)
            {
                if (string.IsNullOrEmpty(scene)) continue;

                var sceneRoutes = stageRouteIndex.GetRoutesForScene(scene);
                if (sceneRoutes == null) continue;

                foreach (var meta in sceneRoutes)
                {
                    if (meta == null) continue;

                    // Optional gating: only count stages unlocked by effectiveWeekCap
                    if (meta.unlockWeek > effectiveWeekCap) continue;

                    int existing = 0;
                    map.TryGetValue(meta.character, out existing);

                    if (meta.stage > existing)
                        map[meta.character] = meta.stage;
                }
            }

            return map;
        }

        private float GetMaxStageFromStats(Character character)
        {
            float maxStage = 0f;

            for (int s = 0; s < stageScenes.Length; s++)
            {
                string scene = stageScenes[s];
                if (string.IsNullOrEmpty(scene)) continue;

                // Must match CharacterStageRouterNode statKey logic exactly. :contentReference[oaicite:1]{index=1}
                string statKey = $"{character} - {scene} - Stage";
                float stage = StatsManager.Get_Numbered_Stat(statKey);

                if (stage > maxStage) maxStage = stage;

                if (verboseLogs)
                    Debug.Log($"[SelectGoodbyeConversationNode] ScanStat char={character} scene={scene} key='{statKey}' val={stage}", gameObject);
            }

            return maxStage;
        }

        private static string SafeLabel(GoodbyeCandidate c, int index)
        {
            if (c == null) return $"Candidate[{index}]";
            if (!string.IsNullOrEmpty(c.debugLabel)) return c.debugLabel;
            if (c.goodbyeConversation != null) return c.goodbyeConversation.name;
            return $"Candidate[{index}]";
        }
    }
}
