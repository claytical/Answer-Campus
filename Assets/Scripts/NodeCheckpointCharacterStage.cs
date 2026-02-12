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
        [Tooltip("If true, stage will be taken from this field. If false, auto-advance using StageRouteIndex.")]
        public bool useManualStage = false;

        public int stage = -1;           // e.g., 2 sets to "Breanna - Library - Stage" = 2
        public bool skipWeeks = false;
        [Header("Route & Agenda")]
        public StageRouteIndex stageRouteIndex;
        public bool completeAgendaEvent = false;

public override void Run_Node()
{
    // 0) Decide which stage we are actually moving to
    int targetStage;

    if (useManualStage)
    {
        // OLD behavior: trust what’s in the inspector
        targetStage = stage;
    }
    else
    {
        // NEW behavior: auto-advance using StageRouteIndex
        if (stageRouteIndex == null)
        {
            Debug.LogWarning(
                $"[Checkpoint] useManualStage is FALSE but stageRouteIndex is null for {character} @ '{scene}'. " +
                "Cannot auto-advance.");
            Finish_Node();
            return;
        }

        string stageKey = $"{character} - {scene} - Stage";
        int currentStage = Mathf.RoundToInt(
            StatsManager.Get_Numbered_Stat(stageKey));

        var nextMeta = stageRouteIndex.GetNextRoute(character, scene, currentStage);
        if (nextMeta != null)
        {
            targetStage = nextMeta.stage;
            Debug.Log($"[Checkpoint] Auto-advancing {character} @ '{scene}' from stage {currentStage} to {targetStage}.");
        }
        else
        {
            Debug.LogWarning(
                $"[Checkpoint] No next StageRouteIndex entry for {character} @ '{scene}' " +
                $"after stage {currentStage}. Cannot auto-advance.");
            Finish_Node();
            return;
        }
    }

    // 1) Advance stage stat
    string finalStageKey = $"{character} - {scene} - Stage";
    StatsManager.Set_Numbered_Stat(finalStageKey, targetStage);
    Debug.Log($"[Checkpoint] Set {finalStageKey} to {targetStage}");

    // 2) Update characterLocations (map pins)
    var list = PlayerPrefsExtra.GetList<CharacterLocation>(
        "characterLocations",
        new List<CharacterLocation>());

    list.RemoveAll(cl => cl.character == character);
    list.Add(new CharacterLocation { character = character, location = scene });
    PlayerPrefsExtra.SetList("characterLocations", list);
    Debug.Log($"[Checkpoint] Added {character} to {scene} in characterLocations");

    // 3) Validate against StageRouteIndex (using final stage)
    if (stageRouteIndex != null &&
        !stageRouteIndex.HasRoute(character, scene, targetStage))
    {
        Debug.LogWarning(
            $"[Checkpoint] No StageRouteIndex route for {character} @ '{scene}' stage {targetStage}. " +
            "Check for typos or missing index entry.");
    }

    // 4) Optionally mark the agenda event completed
    if (completeAgendaEvent)
    {
        string eventId = GameEvents.BuildStageRouteEventId(character, scene, targetStage);
        GameEvents.MarkCustomEventCompleted(eventId);
        Debug.Log($"[Checkpoint] Marked agenda event completed: {eventId}");
    }

    // 5) Conditionally advance the week
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