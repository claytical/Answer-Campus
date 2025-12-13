using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VNEngine
{
    public class NodeCheckpointCharacterStage : Node
    {
        [Header("Week Advancement")]
        public int week;

        [Header("Character Stage Advancement")]
        public Character character;         // e.g., "Breanna"
        public string scene;             // e.g., "Library"
        public int stage = -1;           // e.g., 2 sets to "Breanna - Library - Stage" = 2
        public bool skipWeeks = false;
        [Header("Route & Agenda")]
        public StageRouteIndex stageRouteIndex;
        public bool completeAgendaEvent = false;

        public override void Run_Node()
        {
            string stageKey = $"{character} - {scene} - Stage";
            StatsManager.Set_Numbered_Stat(stageKey, stage);
            Debug.Log($"[Checkpoint] Set {stageKey} to {stage}");

            // Always update characterLocations
            var list = PlayerPrefsExtra.GetList<CharacterLocation>(
                "characterLocations",
                new List<CharacterLocation>());

            list.RemoveAll(cl => cl.character == character);
            list.Add(new CharacterLocation { character = character, location = scene });
            PlayerPrefsExtra.SetList("characterLocations", list);
            Debug.Log($"[Checkpoint] Added {character} to {scene} in characterLocations");

            // Validate against StageRouteIndex
            if (stageRouteIndex != null &&
                !stageRouteIndex.HasRoute(character, scene, stage))
            {
                Debug.LogWarning(
                    $"[Checkpoint] No StageRouteIndex route for {character} @ '{scene}' stage {stage}. " +
                    "Check for typos or missing index entry.");
            }

            // Optionally mark the agenda event completed
            if (completeAgendaEvent)
            {
                string eventId = GameEvents.BuildStageRouteEventId(character, scene, stage);
                GameEvents.MarkCustomEventCompleted(eventId);
                Debug.Log($"[Checkpoint] Marked agenda event completed: {eventId}");
            }

            // Conditionally advance the week
            if (skipWeeks)
            {
                float currentWeek = StatsManager.Get_Numbered_Stat("Week");
                if (currentWeek < week)
                {
                    StatsManager.Set_Numbered_Stat("Week", week);
                    Debug.Log($"[Checkpoint] Advanced week to {week}");
                }
            }

            Finish_Node();
        }


        public override void Button_Pressed() { }

        public override void Finish_Node()
        {
            StopAllCoroutines();
            base.Finish_Node();
        }
    }
}