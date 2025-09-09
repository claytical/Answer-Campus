using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VNEngine
{
    /// <summary>
    /// Place at the top of a scene/conversation to set initial conditions for debugging or writer iteration.
    /// Safe-by-default: can be limited to editor, and/or run once per session.
    /// </summary>
    public class DebugInitNode : Node
    {
        [Header("Safety")]
        public bool editorOnly = true;             // If true, only runs in the Unity Editor
        public bool runOncePerSession = true;      // If true, runs only once per app session
        public string sessionKey = "DebugInitNode_Ran"; // Unique key if you have multiple instances

        [Header("Reset Options")]
        public bool clearMessagesForCurrentScene = false;   // uses Location.cs behavior as a reference pattern
        public bool resetCharacterLocations = false;        // clears map placements
        public bool regenerateFootballSchedule = false;     // force-generate a fresh schedule

        [Header("Semester / Calendar")]
        public bool setWeek = false;
        public int weekValue = 1;                  // 1..16 typical
        public bool setPlayerName = false;
        public string playerName = "Player";

        [Header("Core Traits (Numbers)")]
        public bool setTraits = true;
        public float humor = 0f;
        public float charisma = 0f;
        public float empathy = 0f;
        public float grades = 0f;

        [Header("Character Stage Seeds")]
        public List<CharacterStageSeed> stageSeeds = new List<CharacterStageSeed>();

        [Header("Narrative Flags")]
        public List<BoolFlag> boolFlags = new List<BoolFlag>();     // e.g., "LeftWithLeilani" = true
        public List<NumFlag> numFlags = new List<NumFlag>();        // e.g., "StudyGameScore" = 5
        public List<StringFlag> stringFlags = new List<StringFlag>(); // e.g., "Best Friend" = "Breanna"

        [Header("Map Placement (Optional)")]
//        public List<CharacterLocation> placeCharacters = new List<CharacterLocation>(); // requires your existing CharacterLocation struct

        [Header("Football Progress (Optional)")]
        public bool simulateFootballProgress = false;
        [Tooltip("Mark first N games as played.")]
        public int markFirstNGamesPlayed = 0;
        [Tooltip("Of the first N, how many should be wins?")]
        public int winsAmongMarked = 0;

        [Header("Logging")]
        public bool printSummaryToConsole = true;

        // ---- Serializable helpers ----
        [System.Serializable]
        public class CharacterStageSeed
        {
            public Character character;
            public string sceneName = ""; // must match your router keys, e.g., "Library", "Dining Hall"
            public int stage = 0;         // seed stage number
        }

        [System.Serializable]
        public class BoolFlag
        {
            public string key;
            public bool value;
        }

        [System.Serializable]
        public class NumFlag
        {
            public string key;
            public float value;
        }

        [System.Serializable]
        public class StringFlag
        {
            public string key;
            public string value;
        }

        public override void Run_Node()
        {
#if !UNITY_EDITOR
            if (editorOnly)
            {
                // Skip silently in builds
                Finish_Node();
                return;
            }
#endif
            if (runOncePerSession && PlayerPrefs.GetInt(sessionKey, 0) == 1)
            {
                Finish_Node();
                return;
            }

            // 1) Semester basics
            if (setWeek)
            {
                StatsManager.Set_Numbered_Stat("Week", Mathf.Max(1, weekValue));
            }
            if (setPlayerName)
            {
                StatsManager.Set_String_Stat("Player Name", playerName ?? "Player");
            }

            // 2) Traits
            if (setTraits)
            {
                SetTrait("Humor", humor);
                SetTrait("Charisma", charisma);
                SetTrait("Empathy", empathy);
                SetTrait("Grades", grades);
            }

            // 3) Narrative flags
            foreach (var b in boolFlags)
                if (!string.IsNullOrEmpty(b.key)) StatsManager.Set_Boolean_Stat(b.key, b.value);

            foreach (var n in numFlags)
                if (!string.IsNullOrEmpty(n.key)) StatsManager.Set_Numbered_Stat(n.key, n.value);

            foreach (var s in stringFlags)
                if (!string.IsNullOrEmpty(s.key)) StatsManager.Set_String_Stat(s.key, s.value);

            // 4) Stage seeds: "Character - Scene - Stage" convention
            foreach (var seed in stageSeeds)
            {
                if (!string.IsNullOrEmpty(seed.sceneName))
                {
                    string stageKey = $"{seed.character} - {seed.sceneName} - Stage";
                    StatsManager.Set_Numbered_Stat(stageKey, seed.stage);
                }
            }
            if (printSummaryToConsole)
            {
                Debug.Log($"[DebugInitNode] Applied. Week={StatsManager.Get_Numbered_Stat("Week")} " +
                          $"Humor={GetTrait("Humor")} Charisma={GetTrait("Charisma")} Empathy={GetTrait("Empathy")} Grades={GetTrait("Grades")}");
            }

            if (runOncePerSession) PlayerPrefs.SetInt(sessionKey, 1);
            Finish_Node();
        }

        private static void SetTrait(string key, float value)
        {
            StatsManager.Set_Numbered_Stat(key, value);
        }
        private static float GetTrait(string key)
        {
            return StatsManager.Get_Numbered_Stat(key);
        }

        public override void Finish_Node()
        {
            StopAllCoroutines();
            base.Finish_Node();
        }
    }
}
